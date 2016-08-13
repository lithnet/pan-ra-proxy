using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
