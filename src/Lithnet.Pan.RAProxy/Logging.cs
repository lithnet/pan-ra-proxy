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
        public const int EventIDApiEndpointExceptionWillFailover = 3005;
        public const int EventIDApiEndpointFailover = 3006;


        // Accounting Errors
        public const int EventIDUnknownRadiusHost = 3101;
        public const int EventIDMissingAttribute = 3102;

        // Messaging errors
        public const int EventIDMessageSendFailure = 3201;

        // Info
        public const int EventIDAccountingRequestRecieved = 4001;
        public const int EventIDUserIDUpdateComplete = 4002;


    }
}
