using Akeeba.Unarchiver.EventArgs;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akeeba.Unarchiver.DataWriter;
using System.Threading;
using ICSharpCode.SharpZipLib.BZip2;

namespace Akeeba.Unarchiver.Format
{
    class JPA : Unarchiver
    {
        /// <summary>
        /// Describes the Standard Header of a JPA archive with the Extra Header Field - Spanned Archive Marker extension
        /// </summary>
        public struct JpaArchiveHeader
        {
            public string Signature;
            public ushort HeaderLength;
            public byte MajorVersion;
            public byte MinorVersion;
            public ulong FileCount;
            public ulong UncompressedSize;
            public ulong CompressedSize;
            public ulong TotalParts;
            public ulong TotalLength;
        }

        /// <summary>
        /// Type of an entity in a JPA archive
        /// </summary>
        public enum JpaEntityType
        {
            Directory,
            File,
            Symlink
        };

        /// <summary>
        /// Type of compression of an entity in a JPA archive
        /// </summary>
        public enum JpaCompressionType
        {
            Uncompressed,
            GZip,
            BZip2
        };

        /// <summary>
        /// Describes the Entity Description Block of a JPA archive with the Timestamp Extra Field extension
        /// </summary>
        public struct JpaFileHeader
        {
            public string Signature;
            public ushort BlockLength;
            public ushort LengthOfEntityPath;
            public string EntityPath;
            public JpaEntityType EntityType;
            public JpaCompressionType CompressionType;
            public ulong CompressedSize;
            public ulong UncompressedSize;
            public ulong EntityPermissions;
            public long TimeStamp;
        }

        /// <summary>
        /// Inherit the constructor from the base class
        /// </summary>
        /// <param name="filePath"></param>
        public JPA(string filePath) : base()
        {
            SupportedExtension = "jpa";

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

            try
            {
                // Read the file header
                JpaArchiveHeader archiveHeader = ReadArchiveHeader();

                // Invoke event at start of extraction
                args = new ProgressEventArgs(Progress);
                OnProgressEvent(args);

                while ((CurrentPartNumber != null) && (Progress.FilePosition < archiveHeader.TotalLength))
                {
                    JpaFileHeader fileHeader = ReadFileHeader();

                    // See https://www.codeproject.com/articles/742774/cancel-a-loop-in-a-task-with-cancellationtokens
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
        /// Read the main header of the archive.
        /// </summary>
        /// <returns>
        /// The main header of the archive
        /// </returns>
        public JpaArchiveHeader ReadArchiveHeader()
        {
            JpaArchiveHeader archiveHeader = new JpaArchiveHeader();

            // Initialize parts counter
            archiveHeader.TotalParts = 1;

            // Open the first part
            Open(1);

            archiveHeader.Signature = ReadAsciiString(3);

            if (archiveHeader.Signature != "JPA")
            {
                throw new InvalidArchiveException();
            }

            archiveHeader.HeaderLength = ReadUShort();
            archiveHeader.MajorVersion = ReadByte();
            archiveHeader.MinorVersion = ReadByte();
            archiveHeader.FileCount = ReadULong();
            archiveHeader.UncompressedSize = ReadULong();
            archiveHeader.CompressedSize = ReadULong();

            if (archiveHeader.HeaderLength > 19)
            {
                // We need to loop while we have remaining header bytes
                ushort remainingBytes = (ushort) (archiveHeader.HeaderLength - 19);

                while (remainingBytes > 0)
                {
                    // Do we have an extra header? The next three bytes must be JP followed by 0x01 and the header type
                    byte[] headerSignature = ReadBytes(4);

                    if ((headerSignature[0] != 0x4a) || (headerSignature[1] != 0x50) || (headerSignature[2] != 0x01))
                    {
                        throw new InvalidArchiveException();
                    }

                    // The next two bytes tell us how long this header is, without the 4 byte signature and type but WITH the header length field
                    ushort extraHeaderLength = ReadUShort();

                    // Subtract the read bytes from the remaining bytes in the header.
                    remainingBytes -= (ushort) (4 + extraHeaderLength);

                    // Read the extra header
                    switch (headerSignature[3])
                    {
                        case 0x01:
                            // Spanned Archive Marker header
                            archiveHeader.TotalParts = ReadUShort();

                            break;

                        default:
                            // I have no idea what this is!
                            throw new InvalidArchiveException();
                    }
                }
            }

            // Invoke the archiveInformation event. We need to do some work to get there, through...
            // -- Create a new archive information record
            ArchiveInformation info = new ArchiveInformation();
            // -- Get the total archive size by looping all of its parts
            info.ArchiveSize = 0;

            for (int i = 1; i <= Parts; i++)
            {
                FileInfo fi = new FileInfo(ArchivePath);
                info.ArchiveSize += (ulong) fi.Length;
            }

            archiveHeader.TotalLength = info.ArchiveSize;

            // -- Incorporate bits from the file header
            info.FileCount = archiveHeader.FileCount;
            info.UncompressedSize = archiveHeader.UncompressedSize;
            info.CompressedSize = archiveHeader.CompressedSize;
            // -- Create the event arguments object
            ArchiveInformationEventArgs args = new ArchiveInformationEventArgs(info);
            // -- Finally, invoke the event
            OnArchiveInformationEvent(args);

            // Lastly, return the read archive header
            return archiveHeader;
        }

        /// <summary>
        /// Reads the Entity Description Block in the current position of the JPA archive
        /// </summary>
        /// <returns>The entity description block (file header)</returns>
        public JpaFileHeader ReadFileHeader()
        {
            JpaFileHeader fileHeader = new JpaFileHeader();

            fileHeader.Signature = ReadAsciiString(3);

            if (fileHeader.Signature != "JPF")
            {
                throw new InvalidArchiveException();
            }

            fileHeader.BlockLength = ReadUShort();
            fileHeader.LengthOfEntityPath = ReadUShort();
            fileHeader.EntityPath = ReadUtf8String(fileHeader.LengthOfEntityPath);
            fileHeader.EntityType = (JpaEntityType) Enum.ToObject(typeof(JpaEntityType), ReadSByte());
            fileHeader.CompressionType = (JpaCompressionType) Enum.ToObject(typeof(JpaCompressionType), ReadSByte());
            fileHeader.CompressedSize = ReadULong();
            fileHeader.UncompressedSize = ReadULong();
            fileHeader.EntityPermissions = ReadULong();

            ushort standardEntityBlockLength = (ushort) (21 + fileHeader.LengthOfEntityPath);

            if (fileHeader.BlockLength > standardEntityBlockLength)
            {
                // We need to loop while we have remaining header bytes
                ushort remainingBytes = (ushort) (fileHeader.BlockLength - standardEntityBlockLength);

                while (remainingBytes > 0)
                {
                    // Get the extra header signature
                    byte[] headerSignature = ReadBytes(2);

                    // The next two bytes tell us how long this header is, including the signature
                    ushort extraHeaderLength = ReadUShort();

                    remainingBytes -= extraHeaderLength;

                    if ((headerSignature[0] == 0x00) && (headerSignature[1] == 0x01))
                    {
                        // Timestamp extra field
                        fileHeader.TimeStamp = ReadLong();
                    }
                    else
                    {
                        throw new InvalidArchiveException();
                    }
                }
            }

            // Invoke the OnEntityEvent event. We need to do some work to get there, through...
            // -- Create a new archive information record
            EntityInformation info = new EntityInformation();
            // -- Incorporate bits from the file header
            info.CompressedSize = fileHeader.CompressedSize;
            info.UncompressedSize = fileHeader.UncompressedSize;
            info.StoredName = fileHeader.EntityPath;
            // -- Get the absolute path of the file
            info.AbsoluteName = "";

            if (DataWriter != null)
            {
                info.AbsoluteName = DataWriter.GetAbsoluteFilePath(fileHeader.EntityPath);
            }
            // -- Get some bits from the currently open archive file
            info.PartNumber = CurrentPartNumber ?? 1;
            info.PartOffset = InputStream.Position;
            // -- Create the event arguments object
            EntityEventArgs args = new EntityEventArgs(info);
            // -- Finally, invoke the event
            OnEntityEvent(args);

            // Lastly, return the read file header
            return fileHeader;
        }

        /// <summary>
        /// Processes a data block in a JPA file located in the current file position
        /// </summary>
        /// <param name="dataBlockHeader">The header of the block being processed</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        public void ProcessDataBlock(JpaFileHeader dataBlockHeader, CancellationToken token)
        {
            // Update the archive's progress record
            Progress.FilePosition = (ulong) (SizesOfPartsAlreadyRead + InputStream.Position);
            Progress.RunningCompressed += dataBlockHeader.CompressedSize;
            Progress.RunningUncompressed += dataBlockHeader.CompressedSize;
            Progress.Status = ExtractionStatus.Running;

            // Create the event arguments we'll use when invoking the event
            ProgressEventArgs args = new ProgressEventArgs(Progress);

            // If we don't have a data writer we just need to skip over the data
            if (DataWriter == null)
            {
                if (dataBlockHeader.CompressedSize > 0)
                {
                    SkipBytes((long) dataBlockHeader.CompressedSize);

                    Progress.FilePosition += dataBlockHeader.CompressedSize;
                }

                return;
            }

            // Is this a directory?
            switch (dataBlockHeader.EntityType)
            {
                case JpaEntityType.Directory:
                    DataWriter.MakeDirRecursive(DataWriter.GetAbsoluteFilePath(dataBlockHeader.EntityPath));

                    return;

                case JpaEntityType.Symlink:
                    if (dataBlockHeader.LengthOfEntityPath > 0)
                    {
                        string strTarget = ReadUtf8String(dataBlockHeader.LengthOfEntityPath);
                        DataWriter.MakeSymlink(strTarget, DataWriter.GetAbsoluteFilePath(dataBlockHeader.EntityPath));
                    }

                    return;
            }

            // Begin writing to file
            DataWriter.StartFile(dataBlockHeader.EntityPath);

            // Is this a zero length file?
            if (dataBlockHeader.CompressedSize == 0)
            {
                DataWriter.StopFile();
                return;
            }

            switch (dataBlockHeader.CompressionType)
            {
                case JpaCompressionType.Uncompressed:
                    ProcessUncompressedDataBlock(dataBlockHeader.CompressedSize, token);
                    break;

                case JpaCompressionType.GZip:
                    ProcessGZipDataBlock(dataBlockHeader.CompressedSize, token);
                    break;

                case JpaCompressionType.BZip2:
                    ProcessBZip2DataBlock(dataBlockHeader.CompressedSize, token);
                    break;

            }

            // Stop writing data to the file
            DataWriter.StopFile();

            Progress.FilePosition += dataBlockHeader.CompressedSize;
        }

        /// <summary>
        /// Processes a GZip-compressed data block
        /// </summary>
        /// <param name="compressedLength">Length of the data block in bytes</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        protected void ProcessGZipDataBlock(ulong compressedLength, CancellationToken token)
        {
            Stream memStream = ReadIntoStream((int)compressedLength);

            using (GZipStream decompressStream = new GZipStream(memStream, CompressionMode.Decompress))
            {
                DataWriter.WriteData(decompressStream);
            }
        }

        /// <summary>
        /// Processes a BZip2-compressed data block
        /// </summary>
        /// <param name="compressedLength">Length of the data block in bytes</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        protected void ProcessBZip2DataBlock(ulong compressedLength, CancellationToken token)
        {
            Stream memStream = ReadIntoStream((int)compressedLength);

            using (BZip2InputStream decompressStream = new BZip2InputStream(memStream))
            {
                DataWriter.WriteData(decompressStream);
            }
        }

        /// <summary>
        /// Processes an uncompressed data block
        /// </summary>
        /// <param name="length">Length of the data block in bytes</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        protected void ProcessUncompressedDataBlock(ulong length, CancellationToken token)
        {
            // Batch size for copying data (1 Mb)
            ulong batchSize = 1048576;

            // Copy the data to the destination file one batch at a time
            while (length > 0)
            {
                ulong nextBatch = Math.Min(length, batchSize);
                length -= nextBatch;

                using (Stream readData = ReadIntoStream((int)nextBatch))
                {
                    DataWriter.WriteData(readData);
                }

                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }
            }
        }
    }
}
