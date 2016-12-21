using System.IO;

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
