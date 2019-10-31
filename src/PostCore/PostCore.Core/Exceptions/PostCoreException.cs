using System;

namespace PostCore.Core.Exceptions
{
    public class PostCoreException : Exception
    {
        public PostCoreException()
            : base()
        {}

        public PostCoreException(string message)
            : base(message)
        { }

        public PostCoreException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
