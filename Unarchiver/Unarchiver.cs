//-----------------------------------------------------------------------
// <copyright file="Unarchiver.cs" company="Akeeba Ltd">
// Copyright (c) 2006-2017  Nicholas K. Dionysopoulos / Akeeba Ltd
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
using System.IO.Compression;
using System.Text;
using System.Threading;
using Akeeba.Unarchiver.DataWriter;
using Akeeba.Unarchiver.EventArgs;
using Akeeba.Unarchiver.Resources;
using ICSharpCode.SharpZipLib.BZip2;

namespace Akeeba.Unarchiver
{
    /// <summary>
    /// Abstract unarchiver. Handles all the basic plumbing of an archive reader ("unarchiver") class.
    /// </summary>
    abstract public class Unarchiver: IDisposable
    {
        #region Data types
        /// <summary>
        /// Type of an entity in a JPA archive
        /// </summary>
        public enum TEntityType
        {
            Directory,
            File,
            Symlink
        }

        /// <summary>
        /// Type of compression of an entity in a JPA archive
        /// </summary>
        public enum TCompressionType
        {
            Uncompressed,
            GZip,
            BZip2
        }

        #endregion

        #region Protected Properties
        /// <summary>
        /// The supported file extension of this unarchiver, e.g. "jpa". Must be set in the constructor or the class declaration.
        /// </summary>
        protected string SupportedExtension;

        /// <summary>
        /// The part number we are reading from
        /// </summary>
        protected int? CurrentPartNumber = null;

        /// <summary>
        /// The current extraction progress
        /// </summary>
        protected ExtractionProgress Progress = new ExtractionProgress();
        #endregion

        #region Public Properties
        /// <summary>
        /// Absolute path to the archive file
        /// </summary>
        private string _archivePath = "";

        /// <summary>
        /// The absolute path to the archive file. Always assign the last part of a multipart archive (.jpa, .jps or .zip).
        /// </summary>
        public string ArchivePath
        {
            get
            {
                return _archivePath;
            }

            set
            {
                if (!File.Exists(value))
                {
                    throw new FileNotFoundException(String.Format(Language.ResourceManager.GetString("ERR_UNARCHIVER_FILE_NOT_FOUND"), value));
                }

                string strExtension = Path.GetExtension(value) ?? "";

                if (strExtension.ToUpper().Substring(1) != SupportedExtension.ToUpper())
                {
                    throw new InvalidExtensionException(String.Format(Language.ResourceManager.GetString("ERR_UNARCHIVER_INVALID_EXTENSION"), value, SupportedExtension.ToUpper()));
                }

                _archivePath = value;
            }
        }

        /// <summary>
        /// Number of total archive parts, including the final part (.jpa, .jps or .zip extension)
        /// </summary>
        private int? _parts = null;

        /// <summary>
        /// Total number of archive parts
        /// </summary>
        public int Parts
        {
            get
            {
                if (!_parts.HasValue)
                {
                    try
                    {
                        DiscoverParts();
                    }
                    catch (Exception)
                    {
                        _parts = 0;
                    }
                }

                if (!_parts.HasValue)
                {
                    _parts = 0;
                }

                return (int)_parts;
            }
        }

        /// <summary>
        /// Currently open input file stream
        /// </summary>
        protected Stream InternalInputStream;

        /// <summary>
        /// Returns the input file stream, or Nothing if we're at the EOF of the last part
        /// </summary>
        protected Stream InputStream
        {
            get
            {
                if (InternalInputStream == null)
                {
                    /**
                     * No currently open file. Try to open the current part number. If it's null we open the first part. If it's out of
                     * range we throw an exception.
                     */
                    if (!CurrentPartNumber.HasValue)
                    {
                        CurrentPartNumber = 1;
                        _sizesOfPartsAlreadyRead = 0;
                        Progress.RunningCompressed = 0;
                        Progress.RunningUncompressed = 0;
                    }
                    else if ((CurrentPartNumber <= 0) || (CurrentPartNumber > Parts))
                    {
                        throw new IndexOutOfRangeException(String.Format(Language.ResourceManager.GetString("ERR_UNARCHIVER_PART_NUMBER_OUT_OF_RANGE"), CurrentPartNumber, Parts));
                    }

                    InternalInputStream = new FileStream(GetPartFilename((int)CurrentPartNumber), FileMode.Open);
                }
                else if (InternalInputStream.Position >= InternalInputStream.Length)
                {
                    // We have reached EOF. Open the next part file if applicable.
                    _sizesOfPartsAlreadyRead += InternalInputStream.Length;
                    InternalInputStream.Dispose();
                    CurrentPartNumber += 1;

                    if (CurrentPartNumber <= Parts)
                    {
                        InternalInputStream = new FileStream(GetPartFilename((int)CurrentPartNumber), FileMode.Open);
                    }
                }

                return InternalInputStream;
            }
        }

        /// <summary>
        /// The DataWriter used when extracting a backup archive
        /// </summary>
        protected IDataWriter InternalDataWriter;

        /// <summary>
        /// The DataWriter for extracting a backup archive
        /// </summary>
        public IDataWriter DataWriter
        {
            get
            {
                return InternalDataWriter;
            }
            set
            {
                InternalDataWriter = value;
            }
        }

        /// <summary>
        /// Total size of the part files already read and closed
        /// </summary>
        private long _sizesOfPartsAlreadyRead;

        /// <summary>
        /// Total size of the part files already read (read-only)
        /// </summary>
        public long SizesOfPartsAlreadyRead
        {
            get
            {
                return _sizesOfPartsAlreadyRead;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Public constructor with an archive argument
        /// </summary>
        protected Unarchiver()
        {
            Progress.Status = ExtractionStatus.Idle;
            Progress.FilePosition = 0;
            Progress.RunningCompressed = 0;
            Progress.RunningUncompressed = 0;
        }

        /// <summary>
        /// Gets a suitable unarchiver for the provided archive file
        /// </summary>
        /// <param name="filePath">The absolute filename of the archive file you want to create an unarchiver for</param>
        /// <param name="password">The password for extracting the archive. Currently only implemented for JPS archives.</param>
        /// <returns></returns>
        public static Unarchiver CreateForFile(string filePath, string password = "")
        {
            string extension = Path.GetExtension(filePath) ?? "";
            string strClassName = "Akeeba.Unarchiver.Format." + extension.ToUpper().Substring(1);
            Type classType = Type.GetType(strClassName);

            if (classType == null)
            {
                throw new InvalidExtensionException(String.Format(Language.ResourceManager.GetString("ERR_UNARCHIVER_UNKNOWN_EXTENSION"), extension));
            }

            // Use the System.Activator to spin up the object, passing the filePath as the constructor argument
            return (Unarchiver)Activator.CreateInstance(classType, filePath, password);
        }
        #endregion

        #region Events
        /// <summary>
        /// Event delegate for the archiveInformation event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="a">The event arguments, ArchiveInformationEventArgs type</param>
        public delegate void ArchiveInformationEventHandler(object sender, ArchiveInformationEventArgs a);

        /// <summary>
        /// Event triggered when the archive information is read
        /// </summary>
        public event ArchiveInformationEventHandler ArchiveInformationEvent;

        /// <summary>
        /// Wraps the ArchiveInformationEvent invocation inside a protected virtual method to let derived classes override it.
        /// This method guards against the possibility of a race condition if the last subscriber unsubscribes immediately
        /// after the null check and before the event is raised. So please don't simplify the invocation even if Visual Studio
        /// tells you to.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnArchiveInformationEvent(ArchiveInformationEventArgs e)
        {
            ArchiveInformationEventHandler handler = ArchiveInformationEvent;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }

        /// <summary>
        /// Event delegate for the entity event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="a">The event arguments, EntityEventArgs type</param>
        public delegate void EntityEventHandler(object sender, EntityEventArgs a);

        /// <summary>
        /// Event triggered when an entity header is read
        /// </summary>
        public event EntityEventHandler EntityEvent;

        /// <summary>
        /// Wraps the EntityEvent invocation inside a protected virtual method to let derived classes override it.
        /// This method guards against the possibility of a race condition if the last subscriber unsubscribes immediately
        /// after the null check and before the event is raised. So please don't simplify the invocation even if Visual Studio
        /// tells you to.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnEntityEvent(EntityEventArgs e)
        {
            EntityEventHandler handler = EntityEvent;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }

        /// <summary>
        /// Event delegate for the progress event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="a">The event arguments, ProgressEventArgs type</param>
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs a);

        /// <summary>
        /// Event raised when there is archive extraction progress (after an entity is extracted, end of archive and catchable error conditions)
        /// </summary>
        public event ProgressEventHandler ProgressEvent;

        /// <summary>
        /// Wraps the ProgressEvent invocation inside a protected virtual method to let derived classes override it.
        /// This method guards against the possibility of a race condition if the last subscriber unsubscribes immediately
        /// after the null check and before the event is raised. So please don't simplify the invocation even if Visual Studio
        /// tells you to.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnProgressEvent(ProgressEventArgs e)
        {
            ProgressEventHandler handler = ProgressEvent;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Discovers the number of total parts in this archive set. All parts must live in the same filesystem location.
        /// </summary>
        protected void DiscoverParts()
        {
            _parts = 0;

            if (!File.Exists(_archivePath))
            {
                return;
            }

            string strExtension = Path.GetExtension(_archivePath) ?? "";
            string strNewFile;
            int partNumber = 0;

            /**
             * This loop always runs at least once, therefore _parts will be 1 in single part archives when the check in the while
             * fails right from the first loop. In other words, it guarantees that _parts is always the count of numbered part files
             * plus one (the last .jpa, .jps, or .zip part of the archive set).
             */
            do
            {
                _parts += 1;
                partNumber += 1;

                strNewFile = Path.ChangeExtension(_archivePath, strExtension.Substring(0, 1) + string.Format("{0:00}", partNumber));
            } while (File.Exists(strNewFile));
        }

        /// <summary>
        /// Gets the absolute file path of the archive part file
        /// </summary>
        /// <param name="partNumber">The part number you want to get a filename for. Must be between 1 and the count in the parts property</param>
        /// <returns>The absolute file path to the archive part file</returns>
        protected string GetPartFilename(int partNumber)
        {
            // Range check
            if ((partNumber <= 0) || (partNumber > Parts))
            {
                throw new IndexOutOfRangeException(String.Format(Language.ResourceManager.GetString("ERR_UNARCHIVER_PART_NUMBER_OUT_OF_RANGE"), partNumber, Parts));
            }

            // The n-th (final) part has special handling
            if (partNumber == Parts)
            {
                return _archivePath;
            }

            string strExtension = Path.GetExtension(_archivePath) ?? "";

            return Path.ChangeExtension(_archivePath, strExtension.Substring(0, 1) + string.Format("{0:00}", partNumber));
        }

        /// <summary>
        /// Close the input stream. The next attempted read will start from position 0 of the first part.
        /// </summary>
        protected void Close()
        {
            if ((InternalInputStream != null) && (InternalInputStream is FileStream))
            {
                InternalInputStream.Dispose();
            }

            InternalInputStream = null;

            CurrentPartNumber = null;
            _sizesOfPartsAlreadyRead = 0;
            Progress.RunningCompressed = 0;
            Progress.RunningUncompressed = 0;
        }

        /// <summary>
        /// Opens a specific archive part
        /// </summary>
        /// <param name="partNumber">The part number to open, valid values 1 to parts. Omit to open the first part.</param>
        /// <returns>The FileStream for the specified part number, reset to position 0 (start of file)</returns>
        protected FileStream Open(int partNumber = 1)
        {
            Close();

            // Set up _sizesOfPartsAlreadyRead;
            _sizesOfPartsAlreadyRead = 0;

            for (int i = 1; i < partNumber; i++)
            {
                FileInfo fi = new FileInfo(ArchivePath);
                _sizesOfPartsAlreadyRead += fi.Length;
            }

            CurrentPartNumber = partNumber;

            return (FileStream)InputStream;
        } 
        #endregion

        #region Binary File Access
        /// <summary>
        /// Reads a UTF-8 encoded string off the archive
        /// </summary>
        /// <param name="len">Maximum length of the string, in bytes (might be different than number of characters)</param>
        /// <returns>The string read from the file</returns>
        protected string ReadUtf8String(int len, Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(len, source);

            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Reads an ASCII encoded string off the archive
        /// </summary>
        /// <param name="len">Maximum length of the string, in bytes (same as characters, it's an 8bit encoding)</param>
        /// <returns>The string read from the file</returns>
        protected string ReadAsciiString(int len, Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(len, source);

            return Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Reads a Unicode (UTF-32 little endian) encoded string off the archive
        /// </summary>
        /// <param name="len">Maximum length of the string, in bytes (might be different than number of characters)</param>
        /// <returns>The string read from the file</returns>
        protected string ReadUtf32String(int len, Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(len, source);

            return Encoding.UTF32.GetString(buffer);
        }

        /// <summary>
        /// Reads a signed short integer from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected short ReadShort(Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(2, source);

            return BitConverter.ToInt16(buffer, 0);
        }

        /// <summary>
        /// Reads a signed long integer from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected long ReadLong(Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(4, source);

            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a signed byte from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected sbyte ReadSByte(Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(1, source);

            return (buffer[0] > 127) ? (sbyte)(256 - buffer[0]) : (sbyte)buffer[0];
        }

        /// <summary>
        /// Reads an unsigned short integer from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected ushort ReadUShort(Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(2, source);

            return BitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>
        /// Reads an unsigned long integer from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected ulong ReadULong(Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(4, source);

            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Reads an unsigned byte from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected byte ReadByte(Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(1, source);

            return buffer[0];
        }

        /// <summary>
        /// Reads a chunk of data from the archive into a MemoryStream
        /// </summary>
        /// <param name="len">Up to how many bytes of data should we read</param>
        /// <returns>The MemoryStream with the data read from the archive</returns>
        protected MemoryStream ReadIntoStream(int len, Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = ReadBytes(len, source);

            return new MemoryStream(buffer);
        }

        /// <summary>
        /// Reads up to len bytes from the current and any next archive parts as necessary
        /// </summary>
        /// <param name="len">How many bytes to read in total</param>
        /// <returns>A byte[] array with the content read from the file. May be smaller than len elements!</returns>
        protected byte[] ReadBytes(int len, Stream source = null)
        {
            if (source == null)
            {
                source = InputStream;
            }

            byte[] buffer = new byte[len];

            int readLength = source.Read(buffer, 0, len);

            // Using a different source stream than the InputStream file source? Return whatever we read.
            if (!source.Equals(InputStream))
            {
                return buffer;
            }

            // I've read as many bytes as I wanted. I'm done.
            if (readLength == len)
            {
                return buffer;
            }

            // Have I reached the end of parts?
            if (CurrentPartNumber == Parts)
            {
                Array.Resize(ref buffer, readLength);

                return buffer;
            }

            // Proceed to next part
            Close();
            CurrentPartNumber++;

            // Read the rest of the data and return the combined result
            int remainingRead = len - readLength;
            byte[] restOfBuffer = ReadBytes(remainingRead);
            Array.Copy(restOfBuffer, 0, buffer, readLength, remainingRead);

            return buffer;
        }

        /// <summary>
        /// Skip forward up to len number of bytes. Works across part files automatically.
        /// </summary>
        /// <param name="len">Up to how many byte positions to skip forward.</param>
        protected void SkipBytes(long len)
        {
            long bytesLeft = InputStream.Length - InputStream.Position;

            // We have enough bytes left. Increase position and return.
            if (bytesLeft > len)
            {
                InputStream.Position += len;

                return;
            }

            // We'll either go past EOF or directly on EOF. Close the file and advance the current part
            Close();

            // No more parts.
            if (CurrentPartNumber == Parts)
            {
                return;
            }

            // If we were asked to seek at EOF we have already completed our assignment.
            if (bytesLeft == len)
            {
                return;
            }

            // The position adjustment was for more bytes than what we had in the previous part. We need to seek to the remainder.
            SkipBytes(len - bytesLeft);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Extracts a backup archive.  A DataWriter must be already assigned and configured or an exception will be raised.
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        public abstract void Extract(CancellationToken token);

        /// <summary>
        /// Extracts a backup archive using the specified data writer.
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        /// <param name="dataWriterObject">An object implementing IDataWriter which will be used to handle extracted data</param>
        public void Extract(CancellationToken token, IDataWriter dataWriterObject)
        {
            DataWriter = dataWriterObject;

            Extract(token);
        }

        /// <summary>
        /// Extract the backup archive to the specified filesystem path using direct file writes
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        /// <param name="destinationPath">The path where the archive will be extracted to</param>
        public void Extract(CancellationToken token, string destinationPath)
        {
            IDataWriter myDataWriter = new DirectFileWriter(destinationPath);

            Extract(token, myDataWriter);
        }

        /// <summary>
        /// Go through the archive's contents without extracting data. You can use the fired events to get information about the
        /// entities contained in the archive.
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        public void Scan(CancellationToken token)
        {
            // Setting the data writer to null is detected by the unarchivers, forcing them to skip over the data sections.
            InternalDataWriter = null;

            Extract(token);
        }

        /// <summary>
        /// Test the archive by using the NullWrite data writer which doesn't create any files / folders
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        public void Test(CancellationToken token)
        {
            IDataWriter myDataWriter = new NullWriter();

            Extract(token, myDataWriter);
        }

        #endregion

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    InternalInputStream?.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // (!) Nothing to do for that

                // Set large fields to null.
                _archivePath = null;
                DataWriter = null;

                _disposedValue = true;
            }
        }

        // Override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Unarchiver() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // Uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        #region Common archive handling code
        /// <summary>
        /// Processes a GZip-compressed data block
        /// </summary>
        /// <param name="compressedLength">Length of the data block in bytes</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        protected void ProcessGZipDataBlock(ulong compressedLength, CancellationToken token)
        {
            Stream memStream = ReadIntoStream((int)compressedLength);

            using (DeflateStream decompressStream = new DeflateStream(memStream, CompressionMode.Decompress))
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

        protected void ProcessDataBlock(ulong CompressedSize, ulong UncompressedSize, TCompressionType CompressionType, TEntityType EntityType, string EntityPath, CancellationToken token)
        {
            // Update the archive's progress record
            Progress.FilePosition = (ulong) (SizesOfPartsAlreadyRead + InputStream.Position);
            Progress.RunningCompressed += CompressedSize;
            Progress.RunningUncompressed += UncompressedSize;
            Progress.Status = ExtractionStatus.Running;

            // Create the event arguments we'll use when invoking the event
            ProgressEventArgs args = new ProgressEventArgs(Progress);

            // If we don't have a data writer we just need to skip over the data
            if (DataWriter == null)
            {
                if (CompressedSize > 0)
                {
                    SkipBytes((long) CompressedSize);

                    Progress.FilePosition += CompressedSize;
                }

                return;
            }

            // Is this a directory?
            switch (EntityType)
            {
                case TEntityType.Directory:
                    DataWriter.MakeDirRecursive(DataWriter.GetAbsoluteFilePath(EntityPath));

                    return;

                case TEntityType.Symlink:
                    if (CompressedSize > 0)
                    {
                        string strTarget = ReadUtf8String((int)CompressedSize);
                        DataWriter.MakeSymlink(strTarget, DataWriter.GetAbsoluteFilePath(EntityPath));
                    }

                    return;
            }

            // Begin writing to file
            DataWriter.StartFile(EntityPath);

            // Is this a zero length file?
            if (CompressedSize == 0)
            {
                DataWriter.StopFile();
                return;
            }

            switch (CompressionType)
            {
                case TCompressionType.Uncompressed:
                    ProcessUncompressedDataBlock(CompressedSize, token);
                    break;

                case TCompressionType.GZip:
                    ProcessGZipDataBlock(CompressedSize, token);
                    break;

                case TCompressionType.BZip2:
                    ProcessBZip2DataBlock(CompressedSize, token);
                    break;

            }

            // Stop writing data to the file
            DataWriter.StopFile();

            Progress.FilePosition += CompressedSize;
        }
        #endregion
    }
}
