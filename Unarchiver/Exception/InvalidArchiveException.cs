using System;
using System.Runtime.Serialization;

namespace Akeeba.Unarchiver
{
    /// <summary>
    /// The archive has an invalid format
    /// </summary>
    [Serializable]
    public class InvalidArchiveException : Exception
    {
        public InvalidArchiveException()
        {
        }

        public InvalidArchiveException(string message) : base(message)
        {
        }

        public InvalidArchiveException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidArchiveException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}