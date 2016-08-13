using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    public static class Program
    {
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
            if (Config.DisableCertificateValidation)
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            }

        }

        internal static void Stop()
        {
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
