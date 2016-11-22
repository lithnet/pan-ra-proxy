using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Pan.RAProxy
{
    public class AggregateUserMappingException : AggregateException
    {
        public AggregateUserMappingException(string message, IEnumerable<Exception> exceptions)
            : base(message, exceptions)
        {
        }
    }
}
