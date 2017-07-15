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

        private static BlockingCollection<AccountingRequest> incomingRequests;

        private static Task requestTask;

        private static ManualResetEvent gate = new ManualResetEvent(true);

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

            if (Config.BatchSize > 1)
            {
                Program.StartBatchQueue();
            }
            else
            {
                Program.StartQueue();
            }

            Program.listener = new AccountingListener(Config.AccountingPort);
            Program.listener.Start(Program.cancellationToken.Token);
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
                        Logging.CounterItemsInQueue.Decrement();

                        try
                        {
                            Logging.WriteDebugEntry($"Incoming accounting request received\n{request}", EventLogEntryType.Information, Logging.EventIDAccountingRequestRecieved);
                            Trace.WriteLine($"Request queue lenth: {Program.incomingRequests.Count}");

                            Program.SendMessage(request);
                        }
                        catch (MissingValueException ex)
                        {
                            Logging.WriteDebugEntry($"A radius accounting packet was discarded because it had incomplete information.\n{ex.Message}", EventLogEntryType.Warning, Logging.EventIDMissingAttribute);
                        }
                        catch (Exception ex)
                        {
                            Logging.WriteEntry($"An error occured while submitting the user-id update\n{ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error, Logging.EventIDMessageSendException);
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
            lock (Program.incomingRequests)
            {
                Program.incomingRequests.Add(request);
            }

            Logging.CounterItemsInQueue.Increment();

            // Open the gate if its current closed
            Program.gate.Set();
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

            if (Config.IsUsernameFilterMatch(username.ValueAsString))
            {
                Logging.WriteDebugEntry($"Dropping accounting request for user '{username.ValueAsString}' matching filter", EventLogEntryType.Information, Logging.EventIDFilteredUsernameDropped);
                return;
            }

            Entry e = new Entry
            {
                Username = username.ValueAsString,
                IpAddress = framedIP.ValueAsString
            };

            UidMessage message = new UidMessage { Payload = new Payload() };
            string type;

            switch (accountingType.ValueAsInt)
            {
                case 1:
                    // Accounting start
                    message.Payload.Login = new Login();
                    message.Payload.Login.Entries.Add(e);
                    type = "login";
                    break;

                case 2:
                    // Accounting stop
                    message.Payload.Logout = new Logout();
                    message.Payload.Logout.Entries.Add(e);
                    type = "logout";
                    break;

                default:
                    return;
            }

            try
            {
                Logging.CounterSentPerSecond.Increment();
                message.Send();
                Logging.WriteEntry($"UserID API mapping succeeded\nUsername: {username.ValueAsString}\nIP address: {framedIP.ValueAsString}\nType: {type}", EventLogEntryType.Information, Logging.EventIDUserIDUpdateComplete);
            }
            catch (PanApiException ex)
            {
                Logging.WriteEntry($"The UserID API called failed\nUsername: {username.ValueAsString}\nIP address: {framedIP.ValueAsString}\n{ex.Message}\n{ex.StackTrace}\n{ex.Detail}", EventLogEntryType.Error, Logging.EventIDApiException);
            }
            catch (Exception ex)
            {
                Logging.WriteEntry($"An error occured while submitting the user-id update\nUsername: {username.ValueAsString}\nIP address: {framedIP.ValueAsString}\n{ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error, Logging.EventIDMessageSendException);
            }
        }

        private static void StartBatchQueue()
        {
            Program.incomingRequests = new BlockingCollection<AccountingRequest>();

            Program.requestTask = new Task(() =>
            {
                try
                {
                    List<AccountingRequest> batchedRequests = new List<AccountingRequest>();

                    // Keep processing until the task is cancelled
                    while (!Program.cancellationToken.IsCancellationRequested)
                    {
                        // Batch all messages received
                        while (batchedRequests.Count <= Config.BatchSize)
                        {
                            AccountingRequest nextRequest;

                            // If we still have time, and there's an item in the queue, block until we get it
                            if (Program.incomingRequests.TryTake(out nextRequest, Config.BatchWait, Program.cancellationToken.Token))
                            {
                                // Successfully retrieved an item
                                Logging.CounterItemsInQueue.Decrement();

                                // Store in the list to be sent
                                batchedRequests.Add(nextRequest);

                                // Debug info
                                Trace.WriteLine($"Incoming accounting request dequeued\n{nextRequest}");
                                Trace.WriteLine($"Request queue lenth: {Program.incomingRequests.Count}");
                            }
                            else
                            {
                                // No item to retrieve in elapsed time
                                break;
                            }
                        }

                        // Send what we have
                        if (batchedRequests.Count > 0)
                        {
                            try
                            {
                                Program.SendBatchMessage(batchedRequests);
                            }
                            catch (Exception ex)
                            {
                                Logging.WriteEntry($"An error occured while submitting the user-id update\n{ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error, Logging.EventIDMessageSendException);
                            }
                            finally
                            {
                                batchedRequests.Clear();
                            }
                        }

                        lock (Program.incomingRequests)
                        {
                            if (Program.incomingRequests.Count == 0)
                            {
                                // Close the gate. AddToQueue will re-open the gate when a new item comes in
                                Program.gate.Reset();
                            }
                        }

                        // Wait for the gate to open if its closed. 
                        WaitHandle.WaitAny(new[] { Program.cancellationToken.Token.WaitHandle, Program.gate });
                    }
                }
                catch (OperationCanceledException)
                {
                }

            }, Program.cancellationToken.Token);

            Program.requestTask.Start();
        }

        private static void SendBatchMessage(List<AccountingRequest> requests)
        {
            UidMessage message = new UidMessage { Payload = new Payload() };

            if (requests == null || requests.Count <= 0)
            {
                Trace.WriteLine($"No requests were in the batch to send");
                return;
            }

            foreach (AccountingRequest request in requests)
            {
                try
                {
                    // Try to throw any missing values as exceptions
                    request.Validate();

                    Entry e = new Entry
                    {
                        Username = request.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.UserName)?.ValueAsString,
                        IpAddress = request.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.FramedIPAddress)?.ValueAsString
                    };

                    switch (request.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.AcctStatusType)?.ValueAsInt)
                    {
                        case 1:
                            // Accounting start
                            message.Payload.Login.Entries.Add(e);

                            if (message.Payload.Logout.Entries.Remove(e))
                            {
                                Trace.WriteLine($"Removed logout entry superceeded by login entry {e.Username}:{e.IpAddress}");
                            }

                            break;

                        case 2:
                            // Accounting stop
                            message.Payload.Logout.Entries.Add(e);
                            break;

                        default:
                            Logging.CounterIgnoredPerSecond.Increment();
                            Logging.WriteDebugEntry($"A radius accounting packet was discarded because it was of an unknown type.\n{request}", EventLogEntryType.Warning, Logging.EventIDInvalidRadiusPacket);
                            break;
                    }
                }
                catch (MissingValueException mv)
                {
                    Logging.CounterIgnoredPerSecond.Increment();
                    Logging.WriteDebugEntry($"A radius accounting packet was discarded because it had incomplete information.\n{mv.Message}", EventLogEntryType.Warning, Logging.EventIDInvalidRadiusPacket);
                }
            }

            int sending = message.Payload.Login.Entries.Count + message.Payload.Logout.Entries.Count;

            try
            {
                if (sending <= 0)
                {
                    Trace.WriteLine($"Nothing to send in batch. {requests.Count} were discarded");
                    return;
                }

                Trace.WriteLine($"Sending batch of {sending}");
                message.Send();
                Logging.CounterSentPerSecond.IncrementBy(sending);
                Logging.CounterSentLoginsPerSecond.IncrementBy(message.Payload.Login.Entries.Count);
                Logging.CounterSentLogoutsPerSecond.IncrementBy(message.Payload.Logout.Entries.Count);
                Trace.WriteLine($"Batch completed");
                Logging.WriteEntry($"UserID API mapping succeeded\nLogins: {message.Payload.Login.Entries.Count}\nLogouts: {message.Payload.Logout.Entries.Count}\n", EventLogEntryType.Information, Logging.EventIDUserIDUpdateComplete);
            }
            catch (AggregateUserMappingException ex)
            {
                Logging.WriteEntry($"{ex.Message}\n{ex.InnerExceptions.Count} in batch out of {sending} failed", EventLogEntryType.Error, Logging.EventIDMessageSendException);
                Logging.CounterSentPerSecond.IncrementBy(sending);
                Logging.CounterSentLoginsPerSecond.IncrementBy(message.Payload.Login.Entries.Count);
                Logging.CounterSentLogoutsPerSecond.IncrementBy(message.Payload.Logout.Entries.Count);
            }
            catch (PanApiException ex)
            {
                Logging.WriteEntry($"The UserID API called failed\n{ex.Message}\n{ex.StackTrace}\n{ex.Detail}", EventLogEntryType.Error, Logging.EventIDUnknownApiException);
            }
            catch (Exception ex)
            {
                Logging.WriteEntry($"An error occured while submitting the user-id update\n{ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error, Logging.EventIDMessageSendException);
            }
        }
    }
}
