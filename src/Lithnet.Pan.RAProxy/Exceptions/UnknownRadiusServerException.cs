using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
