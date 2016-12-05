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
    class NullWriter: IDataWriter
    {
        public void makeDirRecursive(string directory)
        {
        }

        public void startFile(string relativePathName)
        {
        }

        public void stopFile()
        {
        }

        public void writeData(byte[] buffer, int count = -1)
        {
        }

        public void writeData(Stream buffer)
        {
        }

        public void makeSymlink(string target, string source)
        {
        }

        public string getAbsoluteFilePath(string relativeFilePath)
        {
            return "";
        }
    }
}
