using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.Pan.RAProxy.RadiusAccounting;

namespace Lithnet.Pan.RAProxy
{
    internal class MessageQueue
    {
        private CancellationToken cancellationToken;

        private readonly BlockingCollection<AccountingRequest> incomingRequests;

        private Task requestTask;

        private readonly ManualResetEvent gate = new ManualResetEvent(true);

        private MemoryCache failedDomainCache = new MemoryCache("failedDomainCache");

        public MessageQueue()
        {
            this.incomingRequests = new BlockingCollection<AccountingRequest>();
        }

        public void Start(CancellationToken token)
        {
            this.cancellationToken = token;

            this.requestTask = new Task(() =>
            {
                try
                {
                    List<AccountingRequest> batchedRequests = new List<AccountingRequest>();

                    // Keep processing until the task is canceled
                    while (!this.cancellationToken.IsCancellationRequested)
                    {
                        // Batch all messages received
                        while (batchedRequests.Count <= Config.BatchSize)
                        {
                            // If we still have time, and there's an item in the queue, block until we get it
                            if (this.incomingRequests.TryTake(out AccountingRequest nextRequest, Config.BatchWait, this.cancellationToken))
                            {
                                // Successfully retrieved an item
                                Logging.CounterItemsInQueue.Decrement();

                                // Store in the list to be sent
                                batchedRequests.Add(nextRequest);

                                // Debug info
                                Trace.WriteLine($"Incoming accounting request dequeued\n{nextRequest}");
                                Trace.WriteLine($"Request queue length: {this.incomingRequests.Count}");
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
                                this.SendBatchMessage(batchedRequests);
                            }
                            catch (Exception ex)
                            {
                                Logging.WriteEntry($"An error occurred while submitting the user-id update\n\n{ex}", EventLogEntryType.Error, Logging.EventIDMessageSendException);
                            }
                            finally
                            {
                                batchedRequests.Clear();
                            }
                        }

                        lock (this.incomingRequests)
                        {
                            if (this.incomingRequests.Count == 0)
                            {
                                // Close the gate. AddToQueue will re-open the gate when a new item comes in
                                this.gate.Reset();
                            }
                        }

                        // Wait for the gate to open if its closed. 
                        WaitHandle.WaitAny(new[] { this.cancellationToken.WaitHandle, this.gate });
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }, this.cancellationToken);

            this.requestTask.Start();
        }

        public void Stop()
        {
            if (this.requestTask != null && this.requestTask.Status == TaskStatus.Running)
            {
                this.requestTask.Wait(10000);
            }
        }

        public void AddToQueue(AccountingRequest request)
        {
            lock (this.incomingRequests)
            {
                this.incomingRequests.Add(request, this.cancellationToken);
            }

            Logging.CounterItemsInQueue.Increment();

            // Open the gate if its current closed
            this.gate.Set();
        }

        private void SendBatchMessage(List<AccountingRequest> requests)
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
                    List<Entry> items = new List<Entry>();

                    string username = this.GetTranslatedUsername(request);

                    if (Config.IsUsernameFilterMatch(username))
                    {
                        Logging.CounterIgnoredPerSecond.Increment();
                        Logging.WriteDebugEntry($"A radius accounting packet was discarded the username matched the regex filter", EventLogEntryType.Warning, Logging.EventIDFilteredUsernameDropped);
                        continue;
                    }

                    foreach (RadiusAttribute v4Address in request.Attributes.Where(t => t.Type == RadiusAttribute.RadiusAttributeType.FramedIPAddress))
                    {
                        Entry e = new Entry
                        {
                            Username = username,
                            IpAddress = v4Address.ValueAsString
                        };

                        items.Add(e);
                    }

                    foreach (RadiusAttribute v6Address in request.Attributes.Where(t => t.Type == RadiusAttribute.RadiusAttributeType.FramedIPv6Address))
                    {
                        Entry e = new Entry
                        {
                            Username = username,
                            IpAddress = v6Address.ValueAsString
                        };

                        items.Add(e);
                    }

                    if (items.Count == 0)
                    {
                        Logging.CounterIgnoredPerSecond.Increment();
                        Logging.WriteDebugEntry($"A radius accounting packet was discarded because it did not contain any IP address entries", EventLogEntryType.Warning, Logging.EventIDInvalidRadiusPacket);
                        continue;
                    }

                    uint requestType = request.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.AcctStatusType).ValueAsInt;

                    switch (requestType)
                    {
                        case 1:
                        case 3:
                            string type;
                            if (requestType == 3)
                            {
                                type = "interim update";
                            }
                            else
                            {
                                type = "accounting start";
                            }

                            foreach (Entry e in items)
                            {
                                Trace.WriteLine($"Added login entry {e.Username}:{e.IpAddress} from {type}");
                                message.Payload.Login.Entries.Add(e);

                                if (message.Payload.Logout.Entries.Remove(e))
                                {
                                    Trace.WriteLine($"Removed logout entry superseded by login entry {e.Username}:{e.IpAddress}");
                                }
                            }

                            break;

                        case 2:
                            // Accounting stop

                            foreach (Entry e in items)
                            {
                                message.Payload.Logout.Entries.Add(e);
                                Trace.WriteLine($"Added logout entry {e.Username}:{e.IpAddress}");
                            }

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
                if (message.Payload.Login.Entries.Count > 0)
                {
                    Trace.WriteLine($"Logins:\n{string.Join("\n", message.Payload.Login.Entries.Select(t => t.Username))}");
                }

                if (message.Payload.Logout.Entries.Count > 0)
                {
                    Trace.WriteLine($"Logouts:\n{string.Join("\n", message.Payload.Logout.Entries.Select(t => t.Username))}");
                }

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
                Logging.WriteEntry($"The UserID API called failed\n\n{ex}", EventLogEntryType.Error, Logging.EventIDUnknownApiException);
            }
            catch (Exception ex)
            {
                Logging.WriteEntry($"An error occurred while submitting the user-id update\n\n{ex}", EventLogEntryType.Error, Logging.EventIDMessageSendException);
            }
        }

        private string GetTranslatedUsername(AccountingRequest request)
        {
            string username = Config.MatchReplace(request.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.UserName)?.ValueAsString);
            string domain = null;

            try
            {
                switch (Config.UsernameTranslationType?.ToLowerInvariant())
                {
                    case "upn":
                        if (!username.Contains("@"))
                        {
                            domain = username.Split('\\')?.FirstOrDefault();

                            if (!string.IsNullOrWhiteSpace(domain) && this.failedDomainCache.Contains(domain))
                            {
                                Trace.WriteLine($"Ignoring lookup for user {username} as domain is in unknown domain cache");
                                return username;
                            }

                            return NativeMethods.TranslateName(username, ExtendedNameFormat.NameSamCompatible, ExtendedNameFormat.NameUserPrincipal);
                        }

                        break;

                    case "nt4":
                        if (!username.Contains("\\"))
                        {
                            domain = username.Split('@')?.LastOrDefault();

                            if (!string.IsNullOrWhiteSpace(domain) && this.failedDomainCache.Contains(domain))
                            {
                                Trace.WriteLine($"Ignoring lookup for user {username} as domain is in unknown domain cache");
                                return username;
                            }

                            return NativeMethods.TranslateName(username, ExtendedNameFormat.NameUserPrincipal, ExtendedNameFormat.NameSamCompatible);
                        }

                        break;
                }
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1317)
                {
                    // Account does not exist;
                    Logging.WriteEntry($"Could not translate name {username} as it was not found in the directory", EventLogEntryType.Warning, Logging.EventIDCouldNotMapNameNotFound);
                }
                else if (ex.NativeErrorCode == 1355)
                {
                    if (!string.IsNullOrWhiteSpace(domain))
                    {
                        this.failedDomainCache.Add(domain, domain, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(8) });
                    }
                    // Domain does not exist;
                    Logging.WriteEntry($"Could not translate name {username} as the domain was unknown. Future usernames from this domain will not be translated", EventLogEntryType.Warning, Logging.EventIDCouldNotMapDomainNotFound);
                }
                else
                {
                    Logging.WriteEntry($"Could not translate name {username} due to an unknown error\n{ex}", EventLogEntryType.Warning, Logging.EventIDCouldNotMapUnknown);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteEntry($"Could not translate name {username} due to an unknown error\n{ex}", EventLogEntryType.Warning, Logging.EventIDCouldNotMapUnknown);
            }

            return username;
        }
    }
}
