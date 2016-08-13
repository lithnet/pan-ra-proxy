using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Pan.RAProxy
{
    internal class Logging
    {
        // Warnings
        public const int EventIDServerCertificateValidationDisabled = 2001;
        
        // Errors
        public const int EventIDApiException = 3001;
        public const int EventIDUnknownApiException = 3002;
        public const int EventIDUnknownApiResponse= 3003;
        public const int EventIDMessageSendException = 3004;


    }
}
