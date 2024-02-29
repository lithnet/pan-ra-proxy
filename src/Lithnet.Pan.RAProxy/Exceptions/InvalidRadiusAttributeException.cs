using System;

namespace Lithnet.Pan.RAProxy
{
    public class InvalidRadiusAttributeException : Exception
    {
        public InvalidRadiusAttributeException()
            : base()
        {
        }

        public InvalidRadiusAttributeException(string message)
            : base(message)
        {
        }

        public InvalidRadiusAttributeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
