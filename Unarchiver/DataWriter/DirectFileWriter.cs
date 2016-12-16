using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Akeeba.Unarchiver.Resources;

#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace Akeeba.Unarchiver.DataWriter
{
    /// <summary>
    /// Handles direct writing to files.
    /// </summary>
    public class DirectFileWriter: IDataWriter, IDisposable
    {
#region Properties
        /// <summary>
        /// Internally stores the absolute filesystem path where files will be written when extracted from the archive.
        /// </summary>
        private string _targetRoot;

        /// <summary>
        /// Where the archive will be extracted to
        /// </summary>
        public string RootDirectory
        {
            get
            {
                return _targetRoot;
            }

            set
            {
                if (!Directory.Exists(value))
                {
                    throw new DirectoryNotFoundException(String.Format(Language.ResourceManager.GetString("ERR_DATAWRITER_DIRECTORY_NOT_FOUND"), value));
                }

                _targetRoot = value;
            }
        }

        /// <summary>
        /// The filestream of the current file (where data will be written to)
        /// </summary>
        private FileStream _outStream;
#endregion

#region Constructors
        /// <summary>
        /// Constructor, allowing you to pass a root directory
        /// </summary>
        /// <param name="extractToDirectory">The root directory where files and folders will be extracted to.</param>
        public DirectFileWriter(string extractToDirectory)
        {
            RootDirectory = extractToDirectory;
        }
#endregion

#region IDataWriter implementation
        /// <summary>
        /// Creates a new directory and all its parent directories
        /// </summary>
        /// <param name="directory">Relative path of the directory being created</param>
        public void MakeDirRecursive(string directory)
        {
            Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Start writing a file. Creates or truncates the file and opens the file stream we're goign to use to write data.
        /// </summary>
        /// <param name="relativePathName">Relative pathname of the file</param>
        public void StartFile(string relativePathName)
        {
            // Close any already open stream
            _outStream?.Close();

            _outStream = new FileStream(GetAbsoluteFilePath(relativePathName), FileMode.Create);
        }

        /// <summary>
        /// Stop writing to a file. Closes the open file stream.
        /// </summary>
        public void StopFile()
        {
            // Close any already open stream
            _outStream?.Close();

            _outStream = null;
        }

        /// <summary>
        /// Append data to the file
        /// </summary>
        /// <param name="buffer">Byte buffer with the data to write</param>
        /// <param name="count">How many bytes to write. A negative number means "as much data as the buffer holds".</param>
        public void WriteData(byte[] buffer, int count = -1)
        {
            if (count < 0)
            {
                count = buffer.Length;
            }

            _outStream.Write(buffer, 0, count);
        }

        /// <summary>
        /// Append data to the file from a stream
        /// </summary>
        /// <param name="buffer">The stream containing the data to write</param>
        public void WriteData(Stream buffer)
        {
            buffer.CopyTo(_outStream, 1048576);
        }

        /// <summary>
        /// Creates a symlink
        /// </summary>
        /// <param name="target">Link target</param>
        /// <param name="source">The relative path of the new link being created</param>
        public void MakeSymlink(string target, string source)
        {
            #if WINDOWS
            // Windows: we use CreateSymbolicLink from kernel32.dll
            int flag = Directory.Exists(target) ? UnsafeNativeMethods.SYMLINK_FLAG_DIRECTORY : 0;
            UnsafeNativeMethods.CreateSymbolicLink(source, target, flag);
            #else
            // Linux, macOS: we run the ln command directly
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "ln";

            StringBuilder sbArgs = new StringBuilder(@"-s ", 512);
            sbArgs.AppendFormat("\"{0}\"", target);
            sbArgs.Append(' ');
            sbArgs.AppendFormat("\"{0}{1}{2}\"", RootDirectory, Path.DirectorySeparatorChar, source);

            p.StartInfo.Arguments = sbArgs.ToString();

            p.Start();
            
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
#endif
        }

        /// <summary>
        /// Returns the absolute filesystem path. If the data writer is not writing to local files return an empty string.
        /// </summary>
        /// <param name="relativeFilePath">Relative path of the file inside the archive, using forward slash as the path separator</param>
        /// <returns></returns>
        public string GetAbsoluteFilePath(string relativeFilePath)
        {
            StringBuilder sb = new StringBuilder(RootDirectory);
            sb.Append(Path.DirectorySeparatorChar);
            sb.Append(relativeFilePath);

            return sb.ToString();
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
                    // Dispose managed state (managed objects).
                    _outStream?.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // None.

                // Set large fields to null.
                // None.

                _disposedValue = true;
            }
        }

        // Override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DirectFileWriter() {
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
    }

    /// <summary>
    /// Contains all the P/Invoke methods which may be dangerous and require a security audit in the calling code
    /// </summary>
    internal static class UnsafeNativeMethods
    {
#if WINDOWS
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static public extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        static public int SYMLINK_FLAG_DIRECTORY = 1;
#endif
    }
}
