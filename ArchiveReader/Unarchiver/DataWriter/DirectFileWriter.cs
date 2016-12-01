using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace Akeeba.Unarchiver.DataWriter
{
    class DirectFileWriter: IDataWriter
    {
#region Properties
        /// <summary>
        /// Internally stores the absolute filesystem path where files will be written when extracted from the archive.
        /// </summary>
        private string extractRoot;

        /// <summary>
        /// Where the archive will be extracted to
        /// </summary>
        public string rootDirectory
        {
            get
            {
                return extractRoot;
            }

            set
            {
                if (!Directory.Exists(value))
                {
                    throw new DirectoryNotFoundException();
                }

                extractRoot = value;
            }
        }

        /// <summary>
        /// The filestream of the current file (where data will be written to)
        /// </summary>
        private FileStream outStream;
#endregion

#region Constructors
        /// <summary>
        /// Constructor, allowing you to pass a root directory
        /// </summary>
        /// <param name="extractToDirectory">The root directory where files and folders will be extracted to.</param>
        public DirectFileWriter(string extractToDirectory)
        {
            rootDirectory = extractToDirectory;
        }
#endregion

#region IDataWriter implementation
        /// <summary>
        /// Creates a new directory and all its parent directories
        /// </summary>
        /// <param name="directory">Relative path of the directory being created</param>
        public void makeDirRecursive(string directory)
        {
            Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Start writing a file. Creates or truncates the file and opens the file stream we're goign to use to write data.
        /// </summary>
        /// <param name="relativePathName">Relative pathname of the file</param>
        public void startFile(string relativePathName)
        {
            // Close any already open stream
            if ((outStream != null) && (outStream is FileStream))
            {
                outStream.Close();
            }

            StringBuilder sb = new StringBuilder(rootDirectory);
            sb.Append(Path.DirectorySeparatorChar);
            sb.Append(relativePathName);

            outStream = new FileStream(sb.ToString(), FileMode.Create);
        }

        /// <summary>
        /// Stop writing to a file. Closes the open file stream.
        /// </summary>
        public void stopFile()
        {
            // Close any already open stream
            if ((outStream != null) && (outStream is FileStream))
            {
                outStream.Close();
            }

            outStream = null;
        }

        /// <summary>
        /// Append data to the file
        /// </summary>
        /// <param name="buffer">Byte buffer with the data to write</param>
        /// <param name="count">How many bytes to write. A negative number means "as much data as the buffer holds".</param>
        public void writeData(byte[] buffer, int count = -1)
        {
            if (count < 0)
            {
                count = buffer.Length;
            }

            outStream.Write(buffer, 0, count);
        }

        /// <summary>
        /// Append data to the file from a stream
        /// </summary>
        /// <param name="buffer">The stream containing the data to write</param>
        public void writeData(Stream buffer)
        {
            buffer.CopyTo(outStream);
        }

        #if WINDOWS
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        static int SYMLINK_FLAG_DIRECTORY = 1;
        #endif

        /// <summary>
        /// Creates a symlink
        /// </summary>
        /// <param name="target">Link target</param>
        /// <param name="source">The relative path of the new link being created</param>
        public void makeSymlink(string target, string source)
        {
            #if WINDOWS
            // Windows: we use CreateSymbolicLink from kernel32.dll
            int flag = Directory.Exists(target) ? SYMLINK_FLAG_DIRECTORY : 0;
            CreateSymbolicLink(source, target, flag);
            #else
            // Linux, macOS: we run the ln command directly
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "ln";

            StringBuilder sbArgs = new StringBuilder(@"-s ", 512);
            sbArgs.AppendFormat("\"{0}\"", target);
            sbArgs.Append(' ');
            sbArgs.AppendFormat("\"{0}{1}{2}\"", rootDirectory, Path.DirectorySeparatorChar, source);

            p.StartInfo.Arguments = sbArgs.ToString();

            p.Start();
            
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
#endif
        }

        #endregion

    }
}
