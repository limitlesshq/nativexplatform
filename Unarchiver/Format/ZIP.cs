//-----------------------------------------------------------------------
// <copyright file="ZIP.cs" company="Akeeba Ltd">
// Copyright (c) 2006-2016  Nicholas K. Dionysopoulos / Akeeba Ltd
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using Akeeba.Unarchiver.EventArgs;
using Akeeba.Unarchiver.Resources;

namespace Akeeba.Unarchiver.Format
{
    internal class ZIP : Unarchiver
    {
        /// <summary>
        /// ZIP End of Central Directory record
        /// </summary>
        public struct ZipEndOfCentralDirectoryRecord
        {
            public ulong Signature;
            public ushort DiskNumber;
            public ushort CDDisk;
            public ushort DiskCDEntries;
            public ushort NumFilesInCD;
            public ulong CDLength;
            public ulong CDOffset;
            public ushort CommentLength;
            public string Comment;
            // This is our own, internal field
            public ulong TotalSize;
        }

        /// <summary>
        /// ZIP Central Directory File Header -- 46 bytes
        /// </summary>
        public struct ZipCentralDirectoryFileHeader
        {
            public ulong Signature;
            public ushort VersionMadeBy;
            public ushort VersionToExtract;
            public ushort Flags;
            public ushort CompressionMethod;
            public ushort LastModTime;
            public ushort LastModDate;
            public ulong CRC32;
            public ulong CompressedSize;
            public ulong UncompressedSize;
            public ushort FileNameLength;
            public ushort ExtraFieldLength;
            public ushort FileCommentLength;
            public ushort DiskNumberStart;
            public ushort InternalFileAttributes;
            public ulong ExternalFileAttributes;
            public ulong RelativeOffset;
            public string Filename;
            public string Comment;
        }

        /// <summary>
        /// ZIP Local File Header -- 30 bytes + variable
        /// </summary>
        public struct ZipLocalFileHeader
        {
            public ulong Signature;
            public ushort VersionToExtract;
            public ushort Flags;
            public ushort CompressionMethod;
            public ushort LastModTime;
            public ushort LastModDate;
            public ulong CRC32;
            public ulong CompressedSize;
            public ulong UncompressedSize;
            public ushort FileNameLength;
            public ushort ExtraFieldLength;
            public string Filename;
        }

        /// <summary>
        /// Inherit the constructor from the base class
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="password">The password for extracting the archive. Currently only implemented for JPS archives.</param>
        public ZIP(string filePath, string password = "") : base()
        {
            SupportedExtension = "zip";

            ArchivePath = filePath;
        }

        /// <summary>
        /// Implements the Extract method which extracts a backup archive. A DataWriter must be already assigned and configured or an
        /// exception will be raised.
        /// </summary>
        public override void Extract(CancellationToken token)
        {
            // Initialize
            Close();
            Progress.FilePosition = 0;
            Progress.RunningCompressed = 0;
            Progress.RunningUncompressed = 0;
            Progress.Status = ExtractionStatus.Running;
            Progress.LastException = null;
            ProgressEventArgs args;

            ZipEndOfCentralDirectoryRecord eocdRecord;

            try
            {
                // Read the End of Central Directory header
                eocdRecord = ReadEndOfCentralDirectory();

                // Reset the position to the very start of the archive
                Close();
                Progress.FilePosition = 0;
                Progress.RunningCompressed = 0;
                Progress.RunningUncompressed = 0;

                // Check for a spanned archive signature
                CheckForSpannedArchiveSignature();

                // Invoke event at start of extraction
                args = new ProgressEventArgs(Progress);
                OnProgressEvent(args);

                while ((CurrentPartNumber != null) && (Progress.FilePosition < eocdRecord.TotalSize))
                {
                    // We have reached the start of the Central Directory, i.e. we're finished extracting
                    if ((CurrentPartNumber == (eocdRecord.CDDisk + 1)) && (Progress.FilePosition == eocdRecord.CDOffset))
                    {
                        break;
                    }

                    ZipLocalFileHeader fileHeader = ReadFileHeader();

                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    ProcessDataBlock(fileHeader, token);

                    args = new ProgressEventArgs(Progress);
                    OnProgressEvent(args);
                }
            }
            catch (OperationCanceledException cancelledException)
            {
                // The operation was cancelled. Set the state to Idle and reset the extraction.
                Close();
                Progress.FilePosition = 0;
                Progress.RunningCompressed = 0;
                Progress.RunningUncompressed = 0;
                Progress.Status = ExtractionStatus.Idle;
                Progress.LastException = cancelledException;

                // Invoke an event notifying susbcribers about the cancelation.
                args = new ProgressEventArgs(Progress);
                OnProgressEvent(args);

                return;
            }
            catch (Exception errorException)
            {
                // Any other exception. Close the option file and set the status to error.
                Close();
                Progress.Status = ExtractionStatus.Error;
                Progress.LastException = errorException;

                // Invoke an event notifying of the error state
                args = new ProgressEventArgs(Progress);
                OnProgressEvent(args);

                return;
            }

            // Invoke an event signaling the end of extraction
            Progress.Status = ExtractionStatus.Finished;
            args = new ProgressEventArgs(Progress);
            OnProgressEvent(args);
        }

        /// <summary>
        /// Locates and reads the End of Central Directory record. It also reads through the entire central directory.
        /// This must be called at the beginning of extraction.
        /// </summary>
        /// <returns>The EOCD record of the archive</returns>
        /// <exception cref="InvalidArchiveException"></exception>
        private ZipEndOfCentralDirectoryRecord ReadEndOfCentralDirectory()
        {
            Open(Parts);
            long localOffset = InputStream.Length - 22;

            /**
             * The EOCD record is 22 to infinity bytes long. Its first 22 bytes are a pre-defined data record, whereas
             * the rest are the ZIP file comment. In order to determine its location relative to the archive's EOF I
             * chose to implement an inneficient backwards sliding window algorithm. We start by reading the last 22
             * bytes of the archive. If the header is not found, we keep sliding backwards, one byte at a time until
             * we either locate the header or reach the BOF. The latter case means we don't have a valid archive. This
             * shouldn't happen, unless the archive was truncated in transit.
             */
            try
            {
                do
                {
                    InputStream.Seek(localOffset, SeekOrigin.Begin);

                    byte[] buffer = ReadBytes(4);

                    if (isEOCD(buffer))
                    {
                        break;
                    }

                    localOffset--;
                } while (localOffset > 0);
            }
            catch (Exception)
            {
                throw new InvalidArchiveException(Language.ResourceManager.GetString("ERR_FORMAT_ZIP_EOCD_NOT_FOUND"));
            }

            // EOCD not found within the last part. That's a violation of the ZIP standard.
            if (localOffset < 0)
            {
                throw new InvalidArchiveException(Language.ResourceManager.GetString("ERR_FORMAT_ZIP_EOCD_NOT_FOUND"));
            }

            // Go back to the EOCD offset and let's read the contents
            ZipEndOfCentralDirectoryRecord eocdRecord = new ZipEndOfCentralDirectoryRecord();
            InputStream.Seek(localOffset, SeekOrigin.Begin);

            eocdRecord.Signature = ReadULong();
            eocdRecord.DiskNumber = ReadUShort();
            eocdRecord.CDDisk = ReadUShort();
            eocdRecord.DiskCDEntries = ReadUShort();
            eocdRecord.NumFilesInCD = ReadUShort();
            eocdRecord.CDLength = ReadULong();
            eocdRecord.CDOffset = ReadULong();
            eocdRecord.CommentLength = ReadUShort();
            eocdRecord.Comment = "";

            if (eocdRecord.CommentLength > 0)
            {
                eocdRecord.Comment = ReadUtf8String(eocdRecord.CommentLength);
            }

            // Now we can go to the beginning of the Central Directory and read its contents. We need to do that to get
            // the comrpessed and uncompressed size counts.
            var info = ReadCentralDirectoryContents(eocdRecord);

            // Invoke the archiveInformation event. We need to do some work to get there, through...

            // -- Get the total archive size by looping all of its parts
            info.ArchiveSize = 0;

            for (int i = 1; i <= Parts; i++)
            {
                FileInfo fi = new FileInfo(ArchivePath);
                info.ArchiveSize += (ulong) fi.Length;
            }

            eocdRecord.TotalSize = info.ArchiveSize;

            // -- Incorporate bits from the file header
            info.FileCount = eocdRecord.NumFilesInCD;
            // -- Create the event arguments object
            ArchiveInformationEventArgs args = new ArchiveInformationEventArgs(info);
            // -- Finally, invoke the event
            OnArchiveInformationEvent(args);

            return eocdRecord;
        }

        /// <summary>
        /// Reads the contents of the archive's Central Directory and returns an ArchiveInformation record with the
        /// CompressedSize and UncompressedSize fields populated from the contents of the Central Directory file header
        /// records.
        /// </summary>
        /// <param name="eocdRecord">The End of Central Directory record which gives us information about the Central Directory's location</param>
        /// <returns>The pre-populated ArchiveInformation record</returns>
        /// <exception cref="InvalidArchiveException"></exception>
        private ArchiveInformation ReadCentralDirectoryContents(ZipEndOfCentralDirectoryRecord eocdRecord)
        {
            // -- Get a new info record
            ArchiveInformation info = new ArchiveInformation();
            info.UncompressedSize = 0;
            info.CompressedSize = 0;

            // -- Open the correct archive part and seek to the first Central Directory record
            Open(eocdRecord.CDDisk + 1);
            InputStream.Seek((long)eocdRecord.CDOffset, SeekOrigin.Begin);

            // -- Loop all entries
            for (int i = 0; i < eocdRecord.NumFilesInCD; i++)
            {
                ZipCentralDirectoryFileHeader cdHeader = new ZipCentralDirectoryFileHeader();

                cdHeader.Signature = ReadULong();

                if (cdHeader.Signature != BitConverter.ToUInt32(new byte[] {0x50, 0x4b, 0x01, 0x02}, 0))
                {
                    throw new InvalidArchiveException(String.Format(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_CD_HEADER_AT_POSITION"), CurrentPartNumber, InputStream.Position - 4));
                }

                cdHeader.VersionMadeBy = ReadUShort();
                cdHeader.VersionToExtract = ReadUShort();
                cdHeader.Flags = ReadUShort();
                cdHeader.CompressionMethod = ReadUShort();
                cdHeader.LastModTime = ReadUShort();
                cdHeader.LastModDate = ReadUShort();
                cdHeader.CRC32 = ReadULong();
                cdHeader.CompressedSize = ReadULong();
                cdHeader.UncompressedSize = ReadULong();
                cdHeader.FileNameLength = ReadUShort();
                cdHeader.ExtraFieldLength = ReadUShort();
                cdHeader.FileCommentLength = ReadUShort();
                cdHeader.DiskNumberStart = ReadUShort();
                cdHeader.InternalFileAttributes = ReadUShort();
                cdHeader.ExternalFileAttributes = ReadULong();
                cdHeader.RelativeOffset = ReadULong();
                cdHeader.Filename = ReadUtf8String(cdHeader.FileNameLength);
                cdHeader.Comment = "";

                if (cdHeader.FileCommentLength > 0)
                {
                    cdHeader.Comment = ReadUtf8String(cdHeader.FileCommentLength);
                }

                info.CompressedSize += cdHeader.CompressedSize;
                info.UncompressedSize += cdHeader.UncompressedSize;
            }
            return info;
        }

        /// <summary>
        /// Reads the first four bytes of the archive and checks if they are a multipart archive's signature. In this
        /// case we have to skip over these four bytes before starting extracting files.
        /// </summary>
        private void CheckForSpannedArchiveSignature()
        {
            Close();
            Open(1);

            ulong signature = ReadULong();

            // Not a multipart archive? Just reopen the first part!
            if ((signature != BitConverter.ToUInt32(new byte[] {0x50, 0x4b, 0x07, 0x08}, 0)) && (signature != BitConverter.ToUInt32(new byte[] {0x50, 0x4b, 0x30, 0x30}, 0)))
            {
                Close();
                Open(1);
            }
        }

        /// <summary>
        /// Reads the local file header in the ZIP archive
        /// </summary>
        /// <returns>The local file header</returns>
        private ZipLocalFileHeader ReadFileHeader()
        {
            ZipLocalFileHeader header = new ZipLocalFileHeader();

            header.Signature = ReadULong();
            header.VersionToExtract = ReadUShort();
            header.Flags = ReadUShort();
            header.CompressionMethod = ReadUShort();
            header.LastModTime = ReadUShort();
            header.LastModDate = ReadUShort();
            header.CRC32 = ReadULong();
            header.CompressedSize = ReadULong();
            header.UncompressedSize = ReadULong();
            header.FileNameLength = ReadUShort();
            header.ExtraFieldLength = ReadUShort();
            header.Filename = ReadUtf8String(header.FileNameLength);

            if (header.ExtraFieldLength > 0)
            {
                SkipBytes(header.ExtraFieldLength);
            }

            // Invoke the OnEntityEvent event. We need to do some work to get there, through...
            // -- Create a new archive information record
            EntityInformation info = new EntityInformation();
            // -- Incorporate bits from the file header
            info.CompressedSize = header.CompressedSize;
            info.UncompressedSize = header.UncompressedSize;
            info.StoredName = header.Filename;
            // -- Get the absolute path of the file
            info.AbsoluteName = "";

            if (DataWriter != null)
            {
                info.AbsoluteName = DataWriter.GetAbsoluteFilePath(header.Filename);
            }
            // -- Get some bits from the currently open archive file
            info.PartNumber = CurrentPartNumber ?? 1;
            info.PartOffset = InputStream.Position;
            // -- Create the event arguments object
            EntityEventArgs args = new EntityEventArgs(info);
            // -- Finally, invoke the event
            OnEntityEvent(args);

            return header;
        }

        /// <summary>
        /// Processes a data block in a JPA file located in the current file position
        /// </summary>
        /// <param name="dataBlockHeader">The header of the block being processed</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        public void ProcessDataBlock(ZipLocalFileHeader dataBlockHeader, CancellationToken token)
        {
            // Get the compression type
            TCompressionType CompressionType = TCompressionType.Uncompressed;

            switch (dataBlockHeader.CompressionMethod)
            {
                case 0:
                    CompressionType = TCompressionType.Uncompressed;
                    break;

                case 8:
                    CompressionType = TCompressionType.GZip;
                    break;

                case 12:
                    CompressionType = TCompressionType.BZip2;
                    break;

                default:
                    throw new InvalidArchiveException(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_COMPRESSION_METHOD"));
            }

            // Decide on the file type
            TEntityType EntityType = TEntityType.File;

            if (dataBlockHeader.Filename.EndsWith("/"))
            {
                EntityType = TEntityType.Directory;
            }
            else if (dataBlockHeader.VersionToExtract == BitConverter.ToUInt16(new byte[] {0x10, 0x03}, 0))
            {
                EntityType = TEntityType.Symlink;
            }

            // Process the data block
            ProcessDataBlock(dataBlockHeader.CompressedSize, dataBlockHeader.UncompressedSize, CompressionType,
                EntityType, dataBlockHeader.Filename, token);
        }

        /// <summary>
        /// Checks if the first four bytes of a buffer indicate the End of Central Directory
        /// </summary>
        /// <param name="buffer">A bbyte buffer</param>
        /// <returns>True if it's the End Of Central Directory signature</returns>
        private bool isEOCD(byte[] buffer)
        {
            return ((buffer[0] == 0x50) && (buffer[1] == 0x4b) && (buffer[2] == 0x05) && (buffer[3] == 0x06));
        }

        /// <summary>
        /// Checks if the first four bytes of a buffer indicate the Start of Central Directory
        /// </summary>
        /// <param name="buffer">A bbyte buffer</param>
        /// <returns>True if it's the Start Of Central Directory signature</returns>
        private bool isSOCD(byte[] buffer)
        {
            return ((buffer[0] == 0x50) && (buffer[1] == 0x4b) && (buffer[2] == 0x01) && (buffer[3] == 0x02));
        }
    }
}
