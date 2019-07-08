using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;
using Lithnet.Pan.RAProxy.RadiusAccounting;

namespace Lithnet.Pan.RAProxy
{
    public static class Program
    {
        private static AccountingListener listener;

        private static CancellationTokenSource cancellationToken;

        private static MessageQueue messageQueue;

        internal const string EventSourceName = "PanRAProxy";

        public static void Main()
        {
            bool runService = !Debugger.IsAttached;

            if (runService)
            {
                ServiceBase[] servicesToRun = new ServiceBase[]
                {
                    new RAProxyService()
                };

                ServiceBase.Run(servicesToRun);
            }
            else
            {
                Start();
                Thread.Sleep(Timeout.Infinite);
            }
        }

        internal static void Start()
        {
            Program.cancellationToken = new CancellationTokenSource();

            if (!EventLog.SourceExists(Program.EventSourceName))
            {
                EventLog.CreateEventSource(new EventSourceCreationData(Program.EventSourceName, "Application"));
            }

            if (Config.DisableCertificateValidation)
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
                Logging.WriteEntry("Server certificate validation has been disabled. The SSL certificate on the Palo Alto device will not be validated", EventLogEntryType.Warning, Logging.EventIDServerCertificateValidationDisabled);
            }

            Program.messageQueue = new MessageQueue();
            Program.messageQueue.Start(Program.cancellationToken.Token);

            Program.listener = new AccountingListener(Program.messageQueue, Config.AccountingPort);
            Program.listener.Start(Program.cancellationToken.Token);
        }

        internal static void Stop()
        {
            if (Program.cancellationToken != null)
            {
                Program.cancellationToken.Cancel();
            }

            Program.listener.Stop();
            Program.messageQueue.Stop();
        }
    }
}
