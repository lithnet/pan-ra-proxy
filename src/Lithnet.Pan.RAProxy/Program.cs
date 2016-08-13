using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;

namespace Lithnet.Pan.RAProxy
{
    public static class Program
    {
        private static AccountingListener listener;

        internal const string EventSourceName = "PanRAProxy";

        public static void Main()
        {
            bool runService = !System.Diagnostics.Debugger.IsAttached;

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
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
        }

        internal static void Start()
        {
            if (!EventLog.SourceExists(Program.EventSourceName))
            {
                EventLog.CreateEventSource(new EventSourceCreationData(Program.EventSourceName, "Application"));
            }

            if (Config.DisableCertificateValidation)
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
                EventLog.WriteEntry(Program.EventSourceName, "Server certificate validation has been disabled. The SSL certificate on the Palo Alto device will not be validated", EventLogEntryType.Warning, Logging.EventIDServerCertificateValidationDisabled);
            }

            Program.listener = new AccountingListener(Config.AccountingPort);
            Program.listener.Start();
        }

        internal static void Stop()
        {
            Program.listener.Stop();
        }

        private static void TestMessage()
        {
            UidMessage message = new UidMessage();
            message.Payload = new Payload();
            message.Payload.Login = new Login();
            message.Payload.Login.Entries = new List<Entry>();
            message.Payload.Login.Entries.Add(new Entry() { IpAddress = "192.168.0.1", Username = "test\\ryan" });

            message.Send();

            message = new UidMessage();
            message.Payload = new Payload();
            message.Payload.Logout = new Logout();
            message.Payload.Logout.Entries = new List<Entry>();
            message.Payload.Logout.Entries.Add(new Entry() { IpAddress = "192.168.0.1", Username = "test\\ryan" });

            message.Send();
        }
    }
}
