using System.IO;

namespace Akeeba.Unarchiver.DataWriter
{
    /// <summary>
    /// Interface for a Data Writer class
    /// </summary>
    public interface IDataWriter
    {
        /// <summary>
        /// Creates a new directory and all its parent directories
        /// </summary>
        /// <param name="directory"></param>
        void MakeDirRecursive(string directory);

        /// <summary>
        /// Notifies the data writer that we'll start dumping data for a new file. The data writer needs to take appropriate action, e.g.
        /// make sure the file is writeable, truncate it as necessary and open its internal file stream.
        /// </summary>
        /// <param name="relativePathName">Relative path to the file, including the file name</param>
        void StartFile(string relativePathName);

        /// <summary>
        /// Notifies the data writer that we have finished dumping data for the file. The data writer needs to take appropriate actions, e.g.
        /// close the file stream.
        /// </summary>
        void StopFile();

        /// <summary>
        /// Instructs the data writer to append data to the file
        /// </summary>
        /// <param name="buffer">Byte buffer with the data to write</param>
        /// <param name="count">How many bytes to write. A negative number means "as much data as the buffer holds".</param>
        void WriteData(byte[] buffer, int count = -1);

        /// <summary>
        /// Instructs the data writer to append data to the file from a stream
        /// </summary>
        /// <param name="buffer">The stream containing the data to write</param>
        void WriteData(Stream buffer);

        /// <summary>
        /// Creates a symbolic link.
        /// </summary>
        /// <param name="target">The link target</param>
        /// <param name="source">The created symlink</param>
        void MakeSymlink(string target, string source);

        /// <summary>
        /// Returns the absolute filesystem path. If the data writer is not writing to local files return an empty string.
        /// </summary>
        /// <param name="relativeFilePath">Relative path of the file inside the archive, using forward slash as the path separator</param>
        /// <returns></returns>
        string GetAbsoluteFilePath(string relativeFilePath);
    }
}
