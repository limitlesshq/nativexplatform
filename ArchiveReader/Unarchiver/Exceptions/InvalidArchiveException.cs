using System;
using System.Runtime.Serialization;

/// <summary>
/// The archive has an invalid format
/// </summary>
namespace Akeeba.Unarchiver
{
    [Serializable]
    internal class InvalidArchiveException : Exception
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