using System;
using System.Runtime.Serialization;

namespace Akeeba.Unarchiver
{
    /// <summary>
    /// An entry in the archive has an invalid header format
    /// </summary>
    [Serializable]
    internal class InvalidHeaderException : Exception
    {
        public InvalidHeaderException()
        {
        }

        public InvalidHeaderException(string message) : base(message)
        {
        }

        public InvalidHeaderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidHeaderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}