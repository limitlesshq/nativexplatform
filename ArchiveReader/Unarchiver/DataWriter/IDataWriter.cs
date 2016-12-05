using System.IO;

namespace Akeeba.Unarchiver.DataWriter
{
    /// <summary>
    /// Interface for a Data Writer class
    /// </summary>
    interface IDataWriter
    {
        /// <summary>
        /// Creates a new directory and all its parent directories
        /// </summary>
        /// <param name="directory"></param>
        void makeDirRecursive(string directory);

        /// <summary>
        /// Notifies the data writer that we'll start dumping data for a new file. The data writer needs to take appropriate action, e.g.
        /// make sure the file is writeable, truncate it as necessary and open its internal file stream.
        /// </summary>
        /// <param name="relativePathName">Relative path to the file, including the file name</param>
        void startFile(string relativePathName);

        /// <summary>
        /// Notifies the data writer that we have finished dumping data for the file. The data writer needs to take appropriate actions, e.g.
        /// close the file stream.
        /// </summary>
        void stopFile();

        /// <summary>
        /// Instructs the data writer to append data to the file
        /// </summary>
        /// <param name="buffer">Byte buffer with the data to write</param>
        /// <param name="count">How many bytes to write. A negative number means "as much data as the buffer holds".</param>
        void writeData(byte[] buffer, int count = -1);

        /// <summary>
        /// Instructs the data writer to append data to the file from a stream
        /// </summary>
        /// <param name="buffer">The stream containing the data to write</param>
        void writeData(Stream buffer);

        /// <summary>
        /// Creates a symbolic link.
        /// </summary>
        /// <param name="target">The link target</param>
        /// <param name="source">The created symlink</param>
        void makeSymlink(string target, string source);

        /// <summary>
        /// Returns the absolute filesystem path. If the data writer is not writing to local files return an empty string.
        /// </summary>
        /// <param name="relativeFilePath">Relative path of the file inside the archive, using forward slash as the path separator</param>
        /// <returns></returns>
        string getAbsoluteFilePath(string relativeFilePath);
    }
}
