using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
