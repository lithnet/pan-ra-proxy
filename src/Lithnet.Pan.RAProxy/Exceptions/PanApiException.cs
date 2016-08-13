using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Pan.RAProxy
{
    public class PanApiException : Exception
    {
        public string Detail { get; set; }


        public PanApiException()
            : base()
        {
            }

        public PanApiException(string message)
            : base(message)
        {
        }

        public PanApiException(string message, string detail)
            :
                this(message)
        {
            this.Detail = detail;
        }
    }
}
