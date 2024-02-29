using System;

namespace Lithnet.Pan.RAProxy
{
    public class UnknownRadiusServerException : Exception
    {

        public UnknownRadiusServerException()
        {
        }

        public UnknownRadiusServerException(string message)
            : base(message)
        {
        }

        public UnknownRadiusServerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
