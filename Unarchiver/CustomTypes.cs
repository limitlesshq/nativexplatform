using System;

namespace Akeeba.Unarchiver
{
    /// <summary>
    /// Possible archive types known in this library
    /// </summary>
    public enum ArchiveType { Unknown = 0, Jpa, Jps, Zip }

    /// <summary>
    /// The unarchiver engine status
    /// </summary>
    public enum ExtractionStatus { Idle  = 0, Running, Finished, Error };

    /// <summary>
    /// Data type for the information of an archive
    /// </summary>
    public struct ArchiveInformation
    {
        /// <summary>
        /// Archive type
        /// </summary>
        public ArchiveType ArchiveType;

        /// <summary>
        /// How many files the archive reports are included in it
        /// </summary>
        public ulong FileCount;

        /// <summary>
        /// Total size of the archive, as reported by the archive
        /// </summary>
        public ulong ArchiveSize;

        /// <summary>
        /// Total size of extracted files, as reported by the archive
        /// </summary>
        public ulong UncompressedSize;

        /// <summary>
        /// Total size of archived files, as reported by the archive
        /// </summary>
        public ulong CompressedSize;
    }

    /// <summary>
    /// Data type for the extraction progress
    /// </summary>
    public struct ExtractionProgress
    {
        /// <summary>
        /// Current position in the archive. This is a running position from the start of the first part, therefore can be greater than the
        /// part size of the archive set.
        /// </summary>
        public ulong FilePosition;

        /// <summary>
        /// Total archived size of files read so far
        /// </summary>
        public ulong RunningCompressed;

        /// <summary>
        /// Total unarchived size of files read so far
        /// </summary>
        public ulong RunningUncompressed;

        /// <summary>
        /// The status of the archiver engine
        /// </summary>
        public ExtractionStatus Status;

        /// <summary>
        /// The last exception thrown by the archiver engine
        /// </summary>
        public Exception LastException;
    }

    /// <summary>
    /// Information about an entity (file, link or folder) stored in the backup archive
    /// </summary>
    public struct EntityInformation
    {
        /// <summary>
        /// File path as stored in the archive
        /// </summary>
        public string StoredName;

        /// <summary>
        /// Calculated file path of the extracted file
        /// </summary>
        public string AbsoluteName;

        /// <summary>
        /// Length of data read from the archive (excluding the header)
        /// </summary>
        public ulong CompressedSize;

        /// <summary>
        /// Real size of the file to be written to disk
        /// </summary>
        public ulong UncompressedSize;

        /// <summary>
        /// Part number where the header starts for this file
        /// </summary>
        public int PartNumber;

        /// <summary>
        /// Offset in the part file where the header starts for this file
        /// </summary>
        public long PartOffset;
    }
}