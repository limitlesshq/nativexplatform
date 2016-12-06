using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Akeeba.Unarchiver.EventArgs;
using Akeeba.Unarchiver.DataWriter;
using System.Threading;

namespace Akeeba.Unarchiver
{
    /// <summary>
    /// Abstract unarchiver. Handles all the basic plumbing of an archive reader ("unarchiver") class.
    /// </summary>
    abstract class Unarchiver
    {
        #region Protected Properties
        /// <summary>
        /// The supported file extension of this unarchiver, e.g. "jpa". Must be set in the constructor or the class declaration.
        /// </summary>
        protected readonly string supportedExtension;

        /// <summary>
        /// The part number we are reading from
        /// </summary>
        protected int? currentPartNumber = null;

        /// <summary>
        /// The current extraction progress
        /// </summary>
        protected extractionProgress progress = new extractionProgress();
        #endregion

        #region Public Properties
        /// <summary>
        /// Absolute path to the archive file
        /// </summary>
        private string propArchivePath = "";

        /// <summary>
        /// The absolute path to the archive file. Always assign the last part of a multipart archive (.jpa, .jps or .zip).
        /// </summary>
        public string archivePath
        {
            get
            {
                return propArchivePath;
            }

            set
            {
                if (!File.Exists(value))
                {
                    throw new FileNotFoundException();
                }

                string strExtension = Path.GetExtension(value);

                if (strExtension.ToUpper() != supportedExtension.ToUpper())
                {
                    throw new InvalidExtensionException();
                }

                propArchivePath = value;
            }
        }

        /// <summary>
        /// Number of total archive parts, including the final part (.jpa, .jps or .zip extension)
        /// </summary>
        private int? propParts = null;

        /// <summary>
        /// Total number of archive parts
        /// </summary>
        public int parts
        {
            get
            {
                if (!propParts.HasValue)
                {
                    try
                    {
                        discoverParts();
                    }
                    catch (Exception)
                    {
                        propParts = 0;
                    }
                }

                return (int)propParts;
            }
        }

        /// <summary>
        /// Currently open input file stream
        /// </summary>
        protected FileStream propInputFile = null;

        /// <summary>
        /// Returns the input file stream, or Nothing if we're at the EOF of the last part
        /// </summary>
        protected FileStream inputFile
        {
            get
            {
                if (propInputFile == null)
                {
                    /**
                     * No currently open file. Try to open the current part number. If it's null we open the first part. If it's out of
                     * range we throw an exception.
                     */
                    if (!currentPartNumber.HasValue)
                    {
                        currentPartNumber = 1;
                        _sizesOfPartsAlreadyRead = 0;
                        progress.runningCompressed = 0;
                        progress.runningUncompressed = 0;
                    }
                    else if ((currentPartNumber <= 0) || (currentPartNumber > parts))
                    {
                        throw new IndexOutOfRangeException();
                    }

                    propInputFile = new FileStream(getPartFilename((int)currentPartNumber), FileMode.Open);
                }
                else if (propInputFile.Position >= propInputFile.Length)
                {
                    // We have reached EOF. Open the next part file if applicable.
                    _sizesOfPartsAlreadyRead += propInputFile.Length;
                    propInputFile.Dispose();
                    currentPartNumber += 1;

                    if (currentPartNumber <= parts)
                    {
                        propInputFile = new FileStream(getPartFilename((int)currentPartNumber), FileMode.Open);
                    }
                }

                return propInputFile;
            }
        }

        /// <summary>
        /// The DataWriter used when extracting a backup archive
        /// </summary>
        protected IDataWriter _dataWriter;

        /// <summary>
        /// The DataWriter for extracting a backup archive
        /// </summary>
        public IDataWriter dataWriter
        {
            get
            {
                return _dataWriter;
            }
            set
            {
                _dataWriter = value;
            }
        }

        /// <summary>
        /// Total size of the part files already read and closed
        /// </summary>
        private long _sizesOfPartsAlreadyRead = 0;

        /// <summary>
        /// TOtal size of the part files already read (read-only)
        /// </summary>
        public long sizesOfPartsAlreadyRead
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
        /// <param name="filePath"></param>
        public Unarchiver(string filePath)
        {
            archivePath = filePath;

            progress.status = extractionStatus.idle;
            progress.filePosition = 0;
            progress.runningCompressed = 0;
            progress.runningUncompressed = 0;
        }

        /// <summary>
        /// Gets a suitable unarchiver for the provided archive file
        /// </summary>
        /// <param name="filePath">The absolute filename of the archive file you want to create an unarchiver for</param>
        /// <returns></returns>
        public static Unarchiver createFor(string filePath)
        {
            string strClassName = "Akeeba.Unarchiver.Format." + Path.GetExtension(filePath).ToUpper();
            Type classType = Type.GetType(strClassName);

            if (classType == null)
            {
                throw new InvalidExtensionException();
            }

            // Use the System.Activator to spin up the object, passing the filePath as the constructor argument
            return (Unarchiver)Activator.CreateInstance(classType, filePath);
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
        public event ArchiveInformationEventHandler archiveInformationEvent;

        /// <summary>
        /// Wraps the archiveInformationEvent invocation inside a protected virtual method to let derived classes override it.
        /// This method guards against the possibility of a race condition if the last subscriber unsubscribes immediately
        /// after the null check and before the event is raised. So please don't simplify the invocation even if Visual Studio
        /// tells you to.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void onArchiveInformationEvent(ArchiveInformationEventArgs e)
        {
            ArchiveInformationEventHandler handler = archiveInformationEvent;

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
        public event EntityEventHandler entityEvent;

        /// <summary>
        /// Wraps the entityEvent invocation inside a protected virtual method to let derived classes override it.
        /// This method guards against the possibility of a race condition if the last subscriber unsubscribes immediately
        /// after the null check and before the event is raised. So please don't simplify the invocation even if Visual Studio
        /// tells you to.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void onEntityEvent(EntityEventArgs e)
        {
            EntityEventHandler handler = entityEvent;

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
        public event ProgressEventHandler progressEvent;

        /// <summary>
        /// Wraps the progressEvent invocation inside a protected virtual method to let derived classes override it.
        /// This method guards against the possibility of a race condition if the last subscriber unsubscribes immediately
        /// after the null check and before the event is raised. So please don't simplify the invocation even if Visual Studio
        /// tells you to.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void onProgressEvent(ProgressEventArgs e)
        {
            ProgressEventHandler handler = progressEvent;

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
        protected void discoverParts()
        {
            propParts = 0;

            if (!File.Exists(propArchivePath))
            {
                return;
            }

            string strExtension = Path.GetExtension(propArchivePath);
            string strNewFile = "";
            int partNumber = 0;

            /**
             * This loop always runs at least once, therefore propParts will be 1 in single part archives when the check in the while
             * fails right from the first loop. In other words, it guarantees that propParts is always the count of numbered part files
             * plus one (the last .jpa, .jps, or .zip part of the archive set).
             */
            do
            {
                propParts += 1;
                partNumber += 1;

                strNewFile = Path.ChangeExtension(propArchivePath, strExtension.Substring(0, 1) + string.Format("{0:00}", partNumber));
            } while (File.Exists(strNewFile));
        }

        /// <summary>
        /// Gets the absolute file path of the archive part file
        /// </summary>
        /// <param name="partNumber">The part number you want to get a filename for. Must be between 1 and the count in the parts property</param>
        /// <returns>The absolute file path to the archive part file</returns>
        protected string getPartFilename(int partNumber)
        {
            // Range check
            if ((partNumber <= 0) || (partNumber > parts))
            {
                throw new IndexOutOfRangeException();
            }

            // The n-th (final) part has special handling
            if (partNumber == parts)
            {
                return propArchivePath;
            }

            string strExtension = Path.GetExtension(propArchivePath);

            return Path.ChangeExtension(propArchivePath, strExtension.Substring(0, 1) + string.Format("{0:00}", partNumber));
        }

        /// <summary>
        /// Close the input stream. The next attempted read will start from position 0 of the first part.
        /// </summary>
        protected void close()
        {
            if ((propInputFile != null) && (propInputFile is FileStream))
            {
                propInputFile.Dispose();
            }

            currentPartNumber = null;
            _sizesOfPartsAlreadyRead = 0;
            progress.runningCompressed = 0;
            progress.runningUncompressed = 0;
        }

        /// <summary>
        /// Opens a specific archive part
        /// </summary>
        /// <param name="partNumber">The part number to open, valid values 1 to parts. Omit to open the first part.</param>
        /// <returns>The FileStream for the specified part number, reset to position 0 (start of file)</returns>
        protected FileStream open(int partNumber = 1)
        {
            close();

            // Set up _sizesOfPartsAlreadyRead;
            _sizesOfPartsAlreadyRead = 0;

            for (int i = 1; i < partNumber; i++)
            {
                FileInfo fi = new FileInfo(archivePath);
                _sizesOfPartsAlreadyRead += fi.Length;
            }

            currentPartNumber = partNumber;

            return inputFile;
        } 
        #endregion

        #region Binary File Access
        /// <summary>
        /// Reads a UTF-8 encoded string off the archive
        /// </summary>
        /// <param name="len">Maximum length of the string, in bytes (might be different than number of characters)</param>
        /// <returns>The string read from the file</returns>
        protected string readUTF8String(int len)
        {
            byte[] buffer = readBytes(len);

            return System.Text.Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Reads an ASCII encoded string off the archive
        /// </summary>
        /// <param name="len">Maximum length of the string, in bytes (same as characters, it's an 8bit encoding)</param>
        /// <returns>The string read from the file</returns>
        protected string readASCIIString(int len)
        {
            byte[] buffer = readBytes(len);

            return System.Text.Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Reads a Unicode (UTF-32 little endian) encoded string off the archive
        /// </summary>
        /// <param name="len">Maximum length of the string, in bytes (might be different than number of characters)</param>
        /// <returns>The string read from the file</returns>
        protected string readUTF32String(int len)
        {
            byte[] buffer = readBytes(len);

            return System.Text.Encoding.UTF32.GetString(buffer);
        }

        /// <summary>
        /// Reads a signed short integer from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected short readShort()
        {
            byte[] buffer = readBytes(2);

            return BitConverter.ToInt16(buffer, 0);
        }

        /// <summary>
        /// Reads a signed long integer from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected long readLong()
        {
            byte[] buffer = readBytes(4);

            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a signed byte from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected sbyte readSByte()
        {
            byte[] buffer = readBytes(1);

            return (buffer[0] > 127) ? (sbyte)(256 - buffer[0]) : (sbyte)buffer[0];
        }

        /// <summary>
        /// Reads an unsigned short integer from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected ushort readUShort()
        {
            byte[] buffer = readBytes(2);

            return BitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>
        /// Reads an unsigned long integer from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected ulong readULong()
        {
            byte[] buffer = readBytes(4);

            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Reads an unsigned byte from the archive
        /// </summary>
        /// <returns>The value read from the archive</returns>
        protected byte readByte()
        {
            byte[] buffer = readBytes(1);

            return buffer[0];
        }

        /// <summary>
        /// Reads a chunk of data from the archive into a MemoryStream
        /// </summary>
        /// <param name="len">Up to how many bytes of data should we read</param>
        /// <returns>The MemoryStream with the data read from the archive</returns>
        protected MemoryStream readIntoStream(int len)
        {
            byte[] buffer = readBytes(len);

            return new MemoryStream(buffer);
        }

        /// <summary>
        /// Reads up to len bytes from the current and any next archive parts as necessary
        /// </summary>
        /// <param name="len">How many bytes to read in total</param>
        /// <returns>A byte[] array with the content read from the file. May be smaller than len elements!</returns>
        protected byte[] readBytes(int len)
        {
            byte[] buffer = new byte[len];

            int readLength = inputFile.Read(buffer, 0, len);

            // I've read as many bytes as I wanted. I'm done.
            if (readLength == len)
            {
                return buffer;
            }

            // Have I reached the end of parts?
            if (currentPartNumber == parts)
            {
                Array.Resize(ref buffer, readLength);

                return buffer;
            }

            // Proceed to next part
            close();
            currentPartNumber++;

            // Read the rest of the data and return the combined result
            int remainingRead = len - readLength;
            byte[] restOfBuffer = readBytes(remainingRead);
            Array.Copy(restOfBuffer, 0, buffer, readLength, remainingRead);

            return buffer;
        }

        /// <summary>
        /// Skip forward up to len number of bytes. Works across part files automatically.
        /// </summary>
        /// <param name="len">Up to how many byte positions to skip forward.</param>
        protected void skipBytes(long len)
        {
            long bytesLeft = inputFile.Length - inputFile.Position;

            // We have enough bytes left. Increase position and return.
            if (bytesLeft > len)
            {
                inputFile.Position += len;

                return;
            }

            // We'll either go past EOF or directly on EOF. Close the file and advance the current part
            close();

            // No more parts.
            if (currentPartNumber == parts)
            {
                return;
            }

            // If we were asked to seek at EOF we have already completed our assignment.
            if (bytesLeft == len)
            {
                return;
            }

            // The position adjustment was for more bytes than what we had in the previous part. We need to seek to the remainder.
            skipBytes(len - bytesLeft);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Extracts a backup archive.  A DataWriter must be already assigned and configured or an exception will be raised.
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        public abstract void extract(CancellationToken token);

        /// <summary>
        /// Extracts a backup archive using the specified data writer.
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        /// <param name="dataWriterObject">An object implementing IDataWriter which will be used to handle extracted data</param>
        public void extract(CancellationToken token, IDataWriter dataWriterObject)
        {
            dataWriter = dataWriterObject;

            extract(token);
        }

        /// <summary>
        /// Extract the backup archive to the specified filesystem path using direct file writes
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        /// <param name="destinationPath">The path where the archive will be extracted to</param>
        public void extract(CancellationToken token, string destinationPath)
        {
            IDataWriter myDataWriter = new DirectFileWriter(destinationPath);

            extract(token, myDataWriter);
        }

        /// <summary>
        /// Go through the archive's contents without extracting data. You can use the fired events to get information about the
        /// entities contained in the archive.
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        public void scan(CancellationToken token)
        {
            // Setting the data writer to null is detected by the unarchivers, forcing them to skip over the data sections.
            _dataWriter = null;

            extract(token);
        }

        /// <summary>
        /// Test the archive by using the NullWrite data writer which doesn't create any files / folders
        /// </summary>
        /// <param name="token">A CancellationToken used by the caller to cancel the execution before it's complete.</param>
        public void test(CancellationToken token)
        {
            IDataWriter myDataWriter = new NullWriter();

            extract(token, myDataWriter);
        }
        #endregion
    }
}
