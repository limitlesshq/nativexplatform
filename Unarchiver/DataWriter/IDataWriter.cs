//-----------------------------------------------------------------------
// <copyright file="IDataWriter.cs" company="Akeeba Ltd">
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
