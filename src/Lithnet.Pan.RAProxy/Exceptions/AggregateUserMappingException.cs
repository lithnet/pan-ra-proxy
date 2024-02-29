using System;
using System.Collections.Generic;

namespace Lithnet.Pan.RAProxy
{
    public class AggregateUserMappingException : AggregateException
    {
        public AggregateUserMappingException(string message, IEnumerable<Exception> exceptions)
            : base(message, exceptions)
        {
        }

        public AggregateUserMappingException() : base()
        {
        }

        public AggregateUserMappingException(string message) : base(message)
        {
        }

        public AggregateUserMappingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public AggregateUserMappingException(IEnumerable<Exception> innerExceptions) : base(innerExceptions)
        {
        }

        public AggregateUserMappingException(params Exception[] innerExceptions) : base(innerExceptions)
        {
        }

        public AggregateUserMappingException(string message, params Exception[] innerExceptions) : base(message, innerExceptions)
        {
        }
    }
}
