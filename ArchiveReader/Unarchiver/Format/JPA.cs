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
    class JPA: Unarchiver
    {
        /// <summary>
        /// Describes the Standard Header of a JPA archive with the Extra Header Field - Spanned Archive Marker extension
        /// </summary>
        public struct jpaArchiveHeader
        {
            public string signature;
            public ushort headerLength;
            public byte majorVersion;
            public byte minorVersion;
            public ulong fileCount;
            public ulong uncompressedSize;
            public ulong compressedSize;
            public ulong totalParts;
            public ulong totalLength;
        }

        /// <summary>
        /// Type of an entity in a JPA archive
        /// </summary>
        public enum jpaEntityType { directory, file, symlink };

        /// <summary>
        /// Type of compression of an entity in a JPA archive
        /// </summary>
        public enum jpaCompressionType { uncompressed, gzip, bzip2 };

        /// <summary>
        /// Describes the Entity Description Block of a JPA archive with the Timestamp Extra Field extension
        /// </summary>
        public struct jpaFileHeader
        {
            public string signature;
            public ushort blockLength;
            public ushort lengthOfEntityPath;
            public string entityPath;
            public jpaEntityType entityType;
            public jpaCompressionType compressionType;
            public ulong compressedSize;
            public ulong uncompressedSize;
            public ulong entityPermissions;
            public long timeStamp;
        }

        /// <summary>
        /// Inherit the constructor from the base class
        /// </summary>
        /// <param name="filePath"></param>
        public JPA(string filePath): base()
        {
            supportedExtension = "jpa";

            archivePath = filePath;
        }

        /// <summary>
        /// Implements the extract method which extracts a backup archive. A DataWriter must be already assigned and configured or an
        /// exception will be raised.
        /// </summary>
        public override void extract(CancellationToken token)
        {
            // Initialize
            close();
            progress.filePosition = 0;
            progress.runningCompressed = 0;
            progress.runningUncompressed = 0;
            progress.status = extractionStatus.running;
            progress.exception = null;
            ProgressEventArgs args;

            try
            {
                // Read the file header
                jpaArchiveHeader archiveHeader = readArchiveHeader();

                // Invoke event at start of extraction
                args = new EventArgs.ProgressEventArgs(progress);
                onProgressEvent(args);

                while (progress.filePosition < archiveHeader.totalLength)
                {
                    jpaFileHeader fileHeader = readFileHeader();

                    // See https://www.codeproject.com/articles/742774/cancel-a-loop-in-a-task-with-cancellationtokens
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    processDataBlock(fileHeader, token);
                }
            }
            catch (OperationCanceledException cancelledException)
            {
                // The operation was cancelled. Set the state to Idle and reset the extraction.
                close();
                progress.filePosition = 0;
                progress.runningCompressed = 0;
                progress.runningUncompressed = 0;
                progress.status = extractionStatus.idle;
                progress.exception = cancelledException;

                // Invoke an event notifying susbcribers about the cancelation.
                args = new EventArgs.ProgressEventArgs(progress);
                onProgressEvent(args);

                return;
            }
            catch (Exception errorException)
            {
                // Any other exception. Close the option file and set the status to error.
                close();
                progress.status = extractionStatus.error;
                progress.exception = errorException;

                // Invoke an event notifying of the error state
                args = new EventArgs.ProgressEventArgs(progress);
                onProgressEvent(args);

                return;
            }

            // Invoke an event signaling the end of extraction
            progress.status = extractionStatus.finished;
            args = new EventArgs.ProgressEventArgs(progress);
            onProgressEvent(args);
        }

        /// <summary>
        /// Read the main header of the archive.
        /// </summary>
        /// <returns>
        /// The main header of the archive
        /// </returns>
        public jpaArchiveHeader readArchiveHeader()
        {
            jpaArchiveHeader archiveHeader = new jpaArchiveHeader();

            // Initialize parts counter
            archiveHeader.totalParts = 1;

            // Open the first part
            open(1);

            archiveHeader.signature = readASCIIString(3);

            if (archiveHeader.signature != "JPA")
            {
                throw new InvalidArchiveException();
            }

            archiveHeader.headerLength = readUShort();
            archiveHeader.majorVersion = readByte();
            archiveHeader.minorVersion = readByte();
            archiveHeader.fileCount = readULong();
            archiveHeader.uncompressedSize = readULong();
            archiveHeader.compressedSize = readULong();

            if (archiveHeader.headerLength > 19)
            {
                // We need to loop while we have remaining header bytes
                ushort remainingBytes = (ushort)(archiveHeader.headerLength - 19);

                while (remainingBytes > 0)
                {
                    // Do we have an extra header? The next three bytes must be JP followed by 0x01 and the header type
                    byte[] headerSignature = readBytes(4);

                    if ((headerSignature[0] != 0x4a) || (headerSignature[1] != 0x50) || (headerSignature[2] == 0x01))
                    {
                        throw new InvalidArchiveException();
                    }

                    // The next two bytes tell us how long this header is, without the 4 byte signature and type but WITH the header length field
                    ushort extraHeaderLength = readUShort();

                    // Subtract the read bytes from the remaining bytes in the header.
                    remainingBytes -= (ushort)(4 + extraHeaderLength);

                    // Read the extra header
                    switch (headerSignature[3])
                    {
                        case 0x01:
                            // Spanned Archive Marker header
                            archiveHeader.totalParts = readUShort();

                            break;

                        default:
                            // I have no idea what this is!
                            throw new InvalidArchiveException();
                    }
                }
            }

            // Invoke the archiveInformation event. We need to do some work to get there, through...
            // -- Create a new archive information record
            archiveInformation info = new archiveInformation();
            // -- Get the total archive size by looping all of its parts
            info.archiveSize = 0;

            for (int i = 1; i < parts; i++)
            {
                FileInfo fi = new FileInfo(archivePath);
                info.archiveSize += (ulong) fi.Length;
            }

            archiveHeader.totalLength = info.archiveSize;

            // -- Incorporate bits from the file header
            info.fileCount = archiveHeader.fileCount;
            info.uncompressedSize = archiveHeader.uncompressedSize;
            info.compressedSize = archiveHeader.compressedSize;
            // -- Create the event arguments object
            ArchiveInformationEventArgs args = new EventArgs.ArchiveInformationEventArgs(info);
            // -- Finally, invoke the event
            onArchiveInformationEvent(args);

            // Lastly, return the read archive header
            return archiveHeader;
        }

        /// <summary>
        /// Reads the Entity Description Block in the current position of the JPA archive
        /// </summary>
        /// <returns>The entity description block (file header)</returns>
        public jpaFileHeader readFileHeader()
        {
            jpaFileHeader fileHeader = new jpaFileHeader();

            fileHeader.signature = readASCIIString(3);

            if (fileHeader.signature != "JPF")
            {
                throw new InvalidArchiveException();
            }

            fileHeader.blockLength = readUShort();
            fileHeader.lengthOfEntityPath = readUShort();
            fileHeader.entityPath = readUTF8String(fileHeader.lengthOfEntityPath);
            fileHeader.entityType = (jpaEntityType)Enum.ToObject(typeof(jpaEntityType), readSByte());
            fileHeader.compressionType = (jpaCompressionType)Enum.ToObject(typeof(jpaCompressionType), readSByte());
            fileHeader.compressedSize = readULong();
            fileHeader.uncompressedSize = readULong();
            fileHeader.entityPermissions = readULong();

            ushort standardEntityBlockLength = (ushort)(21 + fileHeader.lengthOfEntityPath);

            if (fileHeader.blockLength > standardEntityBlockLength)
            {
                // We need to loop while we have remaining header bytes
                ushort remainingBytes = (ushort)(fileHeader.blockLength - standardEntityBlockLength);

                while (remainingBytes > 0)
                {
                    // Get the extra header signature
                    byte[] headerSignature = readBytes(2);

                    // The next two bytes tell us how long this header is, including the signature
                    ushort extraHeaderLength = readUShort();

                    remainingBytes -= extraHeaderLength;

                    if ((headerSignature[0] == 0x00) && (headerSignature[1] == 0x01))
                    {
                        // Timestamp extra field
                        fileHeader.timeStamp = readLong();
                    }
                    else
                    {
                        throw new InvalidArchiveException();
                    }
                }
            }

            // Invoke the onEntityEvent event. We need to do some work to get there, through...
            // -- Create a new archive information record
            entityInformation info = new entityInformation();
            // -- Incorporate bits from the file header
            info.compressedSize = fileHeader.compressedSize;
            info.uncompressedSize = fileHeader.uncompressedSize;
            info.storedName = fileHeader.entityPath;
            // -- Get the absolute path of the file
            info.absoluteName = "";

            if ((dataWriter != null) && (dataWriter is IDataWriter))
            {
                info.absoluteName = dataWriter.getAbsoluteFilePath(fileHeader.entityPath);
            }
            // -- Get some bits from the currently open archive file
            info.partNumber = (int)currentPartNumber;
            info.partOffset = inputFile.Position;
            // -- Create the event arguments object
            EntityEventArgs args = new EventArgs.EntityEventArgs(info);
            // -- Finally, invoke the event
            onEntityEvent(args);

            // Lastly, return the read file header
            return fileHeader;
        }

        /// <summary>
        /// Processes a data block in a JPA file located in the current file position
        /// </summary>
        /// <param name="dataBlockHeader">The header of the block being processed</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        public void processDataBlock(jpaFileHeader dataBlockHeader, CancellationToken token)
        {
            // Update the archive's progress record
            progress.filePosition = (ulong)(sizesOfPartsAlreadyRead + inputFile.Position);
            progress.runningCompressed += dataBlockHeader.compressedSize;
            progress.runningUncompressed += dataBlockHeader.compressedSize;
            progress.status = extractionStatus.running;

            // Create the event arguments we'll use when invoking the event
            ProgressEventArgs args = new EventArgs.ProgressEventArgs(progress);

            // If we don't have a data writer we just need to skip over the data
            if ((dataWriter == null) || !(dataWriter is IDataWriter))
            {
                if (dataBlockHeader.compressedSize > 0)
                {
                    skipBytes((long)dataBlockHeader.compressedSize);
                }

                return;
            }

            // Is this a directory?
            switch (dataBlockHeader.entityType)
            {
                case jpaEntityType.directory:
                    dataWriter.makeDirRecursive(dataWriter.getAbsoluteFilePath(dataBlockHeader.entityPath));

                    // Invoke the onEntityEvent event
                    onProgressEvent(args);

                    return;

                case jpaEntityType.symlink:
                    if (dataBlockHeader.lengthOfEntityPath > 0)
                    {
                        string strTarget = readUTF8String(dataBlockHeader.lengthOfEntityPath);
                        dataWriter.makeSymlink(strTarget, dataWriter.getAbsoluteFilePath(dataBlockHeader.entityPath));
                    }

                    // Invoke the onEntityEvent event
                    onProgressEvent(args);

                    return;
            }

            // Begin writing to file
            dataWriter.startFile(dataBlockHeader.entityPath);

            // Is this a zero length file?
            if (dataBlockHeader.compressedSize == 0)
            {
                dataWriter.stopFile();
                return;
            }

            switch (dataBlockHeader.compressionType)
            {
                case jpaCompressionType.uncompressed:
                    processUncompressedDataBlock(dataBlockHeader.compressedSize, token);
                    break;

                case jpaCompressionType.gzip:
                    processGZipDataBlock(dataBlockHeader.compressedSize, token);
                    break;

                case jpaCompressionType.bzip2:
                    processBZip2DataBlock(dataBlockHeader.compressedSize, token);
                    break;

            }

            // Stop writing data to the file
            dataWriter.stopFile();

            // Invoke the onEntityEvent event
            onProgressEvent(args);
        }

        /// <summary>
        /// Processes a GZip-compressed data block
        /// </summary>
        /// <param name="compressedLength">Length of the data block in bytes</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        protected void processGZipDataBlock(ulong compressedLength, CancellationToken token)
        {
            Stream memStream = readIntoStream((int)compressedLength);

            using (GZipStream decompressStream = new GZipStream(memStream, CompressionMode.Decompress))
            {
                dataWriter.writeData(decompressStream);
            }
        }

        /// <summary>
        /// Processes a BZip2-compressed data block
        /// </summary>
        /// <param name="compressedLength">Length of the data block in bytes</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        protected void processBZip2DataBlock(ulong compressedLength, CancellationToken token)
        {
            Stream memStream = readIntoStream((int)compressedLength);

            using (BZip2InputStream decompressStream = new BZip2InputStream(memStream))
            {
                dataWriter.writeData(decompressStream);
            }
        }

        /// <summary>
        /// Processes an uncompressed data block
        /// </summary>
        /// <param name="length">Length of the data block in bytes</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        protected void processUncompressedDataBlock(ulong length, CancellationToken token)
        {
            // Batch size for copying data (1 Mb)
            ulong batchSize = 1048576;

            // Copy the data to the destination file one batch at a time
            while (length > 0)
            {
                ulong nextBatch = Math.Min(length, batchSize);
                length -= nextBatch;

                using (Stream readData = readIntoStream((int)batchSize))
                {
                    dataWriter.writeData(readData);
                }

                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }
            }
        }
    }
}
