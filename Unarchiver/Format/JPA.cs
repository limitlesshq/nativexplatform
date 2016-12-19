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
using Akeeba.Unarchiver.Resources;

namespace Akeeba.Unarchiver.Format
{
    internal class JPA : Unarchiver
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
        /// Describes the Entity Description Block of a JPA archive with the Timestamp Extra Field extension
        /// </summary>
        public struct JpaFileHeader
        {
            public string Signature;
            public ushort BlockLength;
            public ushort LengthOfEntityPath;
            public string EntityPath;
            public TEntityType EntityType;
            public TCompressionType CompressionType;
            public ulong CompressedSize;
            public ulong UncompressedSize;
            public ulong EntityPermissions;
            public long TimeStamp;
        }

        /// <summary>
        /// Inherit the constructor from the base class
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="password">The password for extracting the archive. Currently only implemented for JPS archives.</param>
        public JPA(string filePath, string password = "") : base()
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

                    ProcessDataBlock(fileHeader.CompressedSize, fileHeader.UncompressedSize, fileHeader.CompressionType, fileHeader.EntityType, fileHeader.EntityPath, token);

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
                throw new InvalidArchiveException(String.Format(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_FILE_TYPE_SIGNATURE"), "JPA"));
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
                        throw new InvalidArchiveException(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_JPA_EXTRA_HEADER"));
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
                            throw new InvalidArchiveException(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_JPA_EXTRA_HEADER"));
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
                throw new InvalidArchiveException(String.Format(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_HEADER_AT_POSITION"), CurrentPartNumber, InputStream.Position - 3));
            }

            fileHeader.BlockLength = ReadUShort();
            fileHeader.LengthOfEntityPath = ReadUShort();
            fileHeader.EntityPath = ReadUtf8String(fileHeader.LengthOfEntityPath);
            fileHeader.EntityType = (TEntityType) Enum.ToObject(typeof(TEntityType), ReadSByte());
            fileHeader.CompressionType = (TCompressionType) Enum.ToObject(typeof(TCompressionType), ReadSByte());
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
                        throw new InvalidArchiveException(String.Format(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_EXTRA_HEADER_AT_POSITION"), CurrentPartNumber, InputStream.Position - 3));
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

    }
}
