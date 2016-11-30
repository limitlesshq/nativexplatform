using System;

namespace Akeeba.Unarchiver
{
    /// <summary>
    /// Possible archive types known in this library
    /// </summary>
    public enum archiveType { unknown = 0, jpa, jps, zip }

    /// <summary>
    /// The unarchiver engine status
    /// </summary>
    public enum extractionStatus { idle  = 0, running, finished, error };

    /// <summary>
    /// Data type for the information of an archive
    /// </summary>
    public struct archiveInformation
    {
        /// <summary>
        /// Archive type
        /// </summary>
        public archiveType archiveType;

        /// <summary>
        /// How many files the archive reports are included in it
        /// </summary>
        public ulong fileCount;

        /// <summary>
        /// Total size of the archive, as reported by the archive
        /// </summary>
        public ulong archiveSize;

        /// <summary>
        /// Total size of extracted files, as reported by the archive
        /// </summary>
        public ulong uncompressedSize;

        /// <summary>
        /// Total size of archived files, as reported by the archive
        /// </summary>
        public ulong compressedSize;
    }

    /// <summary>
    /// Data type for the extraction progress
    /// </summary>
    public struct extractionProgress
    {
        /// <summary>
        /// Current position in the archive. This is a running position from the start of the first part, therefore can be greater than the
        /// part size of the archive set.
        /// </summary>
        public ulong filePosition;

        /// <summary>
        /// Total archived size of files read so far
        /// </summary>
        public ulong runningCompressed;

        /// <summary>
        /// Total unarchived size of files read so far
        /// </summary>
        public ulong runningUncompressed;

        /// <summary>
        /// The status of the archiver engine
        /// </summary>
        public extractionStatus status;

        /// <summary>
        /// The last exception thrown by the archiver engine
        /// </summary>
        public Exception exception;
    }

    /// <summary>
    /// Information about an entity (file, link or folder) stored in the backup archive
    /// </summary>
    public struct entityInformation
    {
        /// <summary>
        /// File path as stored in the archive
        /// </summary>
        public string storedName;

        /// <summary>
        /// Calculated file path of the extracted file
        /// </summary>
        public string absoluteName;

        /// <summary>
        /// Length of data read from the archive (excluding the header)
        /// </summary>
        public ulong compressedSize;

        /// <summary>
        /// Real size of the file to be written to disk
        /// </summary>
        public ulong uncompressedSize;

        /// <summary>
        /// Part number where the header starts for this file
        /// </summary>
        public int partNumber;

        /// <summary>
        /// Offset in the part file where the header starts for this file
        /// </summary>
        public long partOffset;
    }
}