using System;

namespace Lithnet.Pan.RAProxy
{
    public class MissingValueException : Exception
    {
        public MissingValueException()
            : base()
        {
        }

        public MissingValueException(string message)
            : base(message)
        {
        }

        public MissingValueException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
