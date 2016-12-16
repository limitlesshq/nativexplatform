using System;
using System.Runtime.Serialization;

namespace Akeeba.Unarchiver
{
    [Serializable]
    public class InvalidExtensionException : Exception
    {
        public InvalidExtensionException()
        {
        }

        public InvalidExtensionException(string message) : base(message)
        {
        }

        public InvalidExtensionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidExtensionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}