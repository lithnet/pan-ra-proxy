using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

namespace Lithnet.Pan.RAProxy
{
    using System.Collections.Concurrent;
    using System.Deployment.Internal;
    using System.Net;
    using RadiusAccounting;

    public static class Program
    {
        private static AccountingListener listener;

        private static CancellationTokenSource cancellationToken;

        private static BlockingCollection<AccountingRequest> incomingRequests;

        private static Task requestTask;

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
            Program.cancellationToken = new CancellationTokenSource();


            if (!EventLog.SourceExists(Program.EventSourceName))
            {
                EventLog.CreateEventSource(new EventSourceCreationData(Program.EventSourceName, "Application"));
            }

            if (Config.DisableCertificateValidation)
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
                EventLog.WriteEntry(Program.EventSourceName, "Server certificate validation has been disabled. The SSL certificate on the Palo Alto device will not be validated", EventLogEntryType.Warning, Logging.EventIDServerCertificateValidationDisabled);
            }

            Program.StartQueue();

            Program.listener = new AccountingListener(Config.AccountingPort);
            Program.listener.Start();
        }

        internal static void Stop()
        {
            if (Program.cancellationToken != null)
            {
                Program.cancellationToken.Cancel();
            }

            if (Program.requestTask != null && Program.requestTask.Status == TaskStatus.Running)
            {
                Program.requestTask.Wait(10000);
            }

            Program.listener.Stop();
        }

        private static void StartQueue()
        {
            Program.incomingRequests = new BlockingCollection<RadiusAccounting.AccountingRequest>();

            Program.requestTask = new Task(() =>
            {
                try
                {
                    foreach (AccountingRequest request in Program.incomingRequests.GetConsumingEnumerable(Program.cancellationToken.Token))
                    {
                        try
                        {

                            if (Config.DebuggingEnabled)
                            {
                                EventLog.WriteEntry(Program.EventSourceName, $"Incoming accounting request received\n{request}", EventLogEntryType.Information, Logging.EventIDAccountingRequestRecieved);
                            }

                            Program.SendMessage(request);
                        }
                        catch (MissingValueException ex)
                        {
                            if (Config.DebuggingEnabled)
                            {
                                EventLog.WriteEntry(Program.EventSourceName, $"A radius accounting packet was discarded because it had incomplete information.\n{ex.Message}", EventLogEntryType.Warning, Logging.EventIDMessageSendFailure);
                            }
                        }
                        catch (Exception ex)
                        {
                            EventLog.WriteEntry(Program.EventSourceName, $"An error occured while submitting the user-id update\n{ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error, Logging.EventIDMessageSendFailure);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }

            }, Program.cancellationToken.Token);

            Program.requestTask.Start();
        }

        internal static void AddToQueue(AccountingRequest request)
        {
            Program.incomingRequests.Add(request);
        }

        private static void SendMessage(AccountingRequest request)
        {
            RadiusAttribute accountingType = request.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.AcctStatusType);
            RadiusAttribute framedIP = request.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.FramedIPAddress);
            RadiusAttribute username = request.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.UserName);

            if (accountingType == null)
            {
                throw new MissingValueException("The Acct-Status-Type attribute was not present");
            }

            if (framedIP == null)
            {
                throw new MissingValueException("The Framed-IP-Address attribute was not present");
            }

            if (username == null)
            {
                throw new MissingValueException("The Username attribute was not present");
            }

            Entry e = new Entry
            {
                Username = username.ValueAsString,
                IpAddress = framedIP.ValueAsString
            };

            UidMessage message = new UidMessage { Payload = new Payload() };

            switch (accountingType.ValueAsInt)
            {
                case 1:
                    // Accounting start
                    message.Payload.Login = new Login();
                    message.Payload.Login.Entries.Add(e);
                    break;

                case 2:
                    // Accounting stop
                    message.Payload.Logout = new Logout();
                    message.Payload.Logout.Entries.Add(e);
                    break;

                default:
                    return;
            }

            try
            {
                message.Send();
            }
            catch (PanApiException ex)
            {
                EventLog.WriteEntry(Program.EventSourceName, $"The UserID API called failed\nUsername:{username.ValueAsString}\nIP address:{framedIP.ValueAsString}\n{ex.Message}\n{ex.StackTrace}\n{ex.Detail}", EventLogEntryType.Error, Logging.EventIDMessageSendFailure);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Program.EventSourceName, $"An error occured while submitting the user-id update\nUsername:{username.ValueAsString}\nIP address:{framedIP.ValueAsString}\n{ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error, Logging.EventIDMessageSendFailure);
            }
        }
    }
}
