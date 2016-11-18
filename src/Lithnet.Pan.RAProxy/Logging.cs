using System.Diagnostics;

namespace Lithnet.Pan.RAProxy
{
    internal class Logging
    {
        // Warnings
        public const int EventIDServerCertificateValidationDisabled = 2001;

        // Api Errors
        public const int EventIDApiException = 3001;
        public const int EventIDUnknownApiException = 3002;
        public const int EventIDUnknownApiResponse = 3003;
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

        public static PerformanceCounter CounterReceivedPerSecond { get; }

        public static PerformanceCounter CounterSentPerSecond { get; }

        public static PerformanceCounter CounterItemsInQueue { get; }

        public static void WriteEntry(string message, EventLogEntryType type, int eventID)
        {
            Trace.WriteLine(message);
            EventLog.WriteEntry(Program.EventSourceName, message, type, eventID);

        }

        public static void WriteDebugEntry(string message, EventLogEntryType type, int eventID)
        {
            Trace.WriteLine(message);
            if (Config.DebuggingEnabled)
            {
                EventLog.WriteEntry(Program.EventSourceName, message, type, eventID);
            }
        }



        static Logging()
        {
            Logging.CounterReceivedPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Requests received / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterSentPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Requests sent / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterItemsInQueue = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Requests in queue",
                MachineName = ".",
                ReadOnly = false
            };
        }
    }
}
