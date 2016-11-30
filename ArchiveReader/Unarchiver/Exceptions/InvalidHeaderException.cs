using System;
using System.Runtime.Serialization;

/// <summary>
/// An entry in the archive has an invalid header format
/// </summary>
namespace Akeeba.Unarchiver
{
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