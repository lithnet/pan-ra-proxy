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
        
        // Api Errors
        public const int EventIDApiException = 3001;
        public const int EventIDUnknownApiException = 3002;
        public const int EventIDUnknownApiResponse= 3003;
        public const int EventIDMessageSendException = 3004;

        // Accounting Errors
        public const int EventIDUnknownRadiusHost = 3101;
        public const int EventIDMissingAttribute = 3102;

        // Messaging errors
        public const int EventIDMessageSendFailure = 3201;

        // Info
        public const int EventIDAccountingRequestRecieved = 4001;


    }
}
