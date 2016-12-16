using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akeeba.Unarchiver.DataWriter
{
    /// <summary>
    /// Null data writer. Does not write anything, anywhere. Used for testing archive extraction.
    /// </summary>
    public sealed class NullWriter: IDataWriter
    {
        public void MakeDirRecursive(string directory)
        {
        }

        public void StartFile(string relativePathName)
        {
        }

        public void StopFile()
        {
        }

        public void WriteData(byte[] buffer, int count = -1)
        {
        }

        public void WriteData(Stream buffer)
        {
        }

        public void MakeSymlink(string target, string source)
        {
        }

        public string GetAbsoluteFilePath(string relativeFilePath)
        {
            return "";
        }
    }
}
