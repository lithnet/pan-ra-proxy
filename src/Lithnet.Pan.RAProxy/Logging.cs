using System.Diagnostics;

namespace Lithnet.Pan.RAProxy
{
    internal static class Logging
    {
        // Warnings
        public const int EventIDServerCertificateValidationDisabled = 2001;

        // Name translation warnings
        public const int EventIDCouldNotMapNameNotFound = 2101;
        public const int EventIDCouldNotMapDomainNotFound = 2102;
        public const int EventIDCouldNotMapUnknown = 2199;

        // Api Errors
        public const int EventIDApiException = 3001;
        public const int EventIDUnknownApiException = 3002;
        public const int EventIDUnknownApiResponse = 3003;
        public const int EventIDMessageSendException = 3004;
        public const int EventIDApiEndpointExceptionWillFailover = 3005;
        public const int EventIDApiEndpointFailover = 3006;
        public const int EventIDApiUserIDMappingLoginFailed = 3007;
        public const int EventIDApiUserIDMappingLogoutFailed = 3008;

        // Accounting Errors
        public const int EventIDUnknownRadiusHost = 3101;
        public const int EventIDMissingAttribute = 3102;
        public const int EventIDInvalidRadiusPacket = 3202;

        // Info
        public const int EventIDAccountingRequestReceived = 4001;
        public const int EventIDUserIDUpdateComplete = 4002;
        public const int EventIDFilteredUsernameDropped = 4003;

        public static PerformanceCounter CounterReceivedPerSecond { get; }

        public static PerformanceCounter CounterReceivedAccountingStartPerSecond { get; }

        public static PerformanceCounter CounterReceivedAccountingStopPerSecond { get; }

        public static PerformanceCounter CounterReceivedAccountingOtherPerSecond { get; }

        public static PerformanceCounter CounterReceivedDiscardedPerSecond { get; }

        public static PerformanceCounter CounterSentPerSecond { get; }

        public static PerformanceCounter CounterSentLoginsPerSecond { get; }

        public static PerformanceCounter CounterSentLogoutsPerSecond { get; }

        public static PerformanceCounter CounterIgnoredPerSecond { get; }

        public static PerformanceCounter CounterItemsInQueue { get; }

        public static PerformanceCounter CounterFailedMappingsPerSecond { get; }

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
                CounterName = "Accounting requests received / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterReceivedDiscardedPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Accounting requests discarded / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterReceivedAccountingStartPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Accounting start requests received / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterReceivedAccountingStopPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Accounting stop requests received / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterReceivedAccountingOtherPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Accounting other requests received / second",
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

            Logging.CounterSentLoginsPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Requests sent login / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterSentLogoutsPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Requests sent logout / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterIgnoredPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Requests ignored / second",
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

            Logging.CounterFailedMappingsPerSecond = new PerformanceCounter
            {
                CategoryName = "PANRAProxy",
                CounterName = "Failed mappings / second",
                MachineName = ".",
                ReadOnly = false
            };

            Logging.CounterItemsInQueue.RawValue = 0;
        }
    }
}
