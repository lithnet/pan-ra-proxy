using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    using System.Diagnostics;
    using System.Net;

    internal static class Config
    {
        private static IList<PanApiEndpoint> apiEndpoints;

        private static IEnumerator<PanApiEndpoint> activeEndpointEnumerator;

        private static RAProxyConfigurationSection section = ConfigurationManager.GetSection(RAProxyConfigurationSection.SectionName) as RAProxyConfigurationSection;

        private static Dictionary<string, string> cachedSecrets = new Dictionary<string, string>();

        public static IList<PanApiEndpoint> ApiEndpoints
        {
            get
            {
                if (Config.apiEndpoints == null)
                {
                    Config.apiEndpoints = Config.section.PanApi.OfType<PanApiEndpoint>().ToList();
                }

                return Config.apiEndpoints;
            }
        }

        public static bool CanFailover => Config.ApiEndpoints.Count > 1;

        public static PanApiEndpoint ActiveEndPoint
        {
            get
            {
                if (Config.activeEndpointEnumerator == null)
                {
                    Config.activeEndpointEnumerator = Config.ApiEndpoints.GetEnumerator();
                    Config.activeEndpointEnumerator.MoveNext();
                }

                if (Config.activeEndpointEnumerator.Current == null)
                {
                    Config.activeEndpointEnumerator.Reset();
                    Config.activeEndpointEnumerator.MoveNext();
                }

                return Config.activeEndpointEnumerator.Current;
            }
        }

        public static void Failover()
        {
            if (!Config.activeEndpointEnumerator.MoveNext())
            {
                Config.activeEndpointEnumerator.Reset();
                Config.activeEndpointEnumerator.MoveNext();
            }

            EventLog.WriteEntry(Program.EventSourceName, $"Failed over to API endpoint {Config.ActiveEndPoint.ApiUri}\n", EventLogEntryType.Warning, Logging.EventIDApiEndpointFailover);
        }

        public static bool DisableCertificateValidation => Config.section.PanApi.DisableCertificateValidation;

        public static int AccountingPort => Config.section.RadiusClient.Port;

        public static bool DebuggingEnabled => Config.section.DebuggingEnabled;

        public static string GetSecretForIP(IPAddress address)
        {
            string addressStringForm = address.ToString();

            if (Config.cachedSecrets.ContainsKey(addressStringForm))
            {
                return Config.cachedSecrets[addressStringForm];
            }

            foreach (RadiusServerSection radiusServer in section.RadiusServers)
            {
                if (radiusServer.GetIpAddresses().Any(t => t.Equals(address)))
                {
                    Config.cachedSecrets.Add(addressStringForm, radiusServer.Secret);
                    return radiusServer.Secret;
                }
            }

            EventLog.WriteEntry(Program.EventSourceName, $"A RADIUS message was received from an unknown source {address} and was discarded", EventLogEntryType.Error, Logging.EventIDUnknownRadiusHost);

            return null;
        }
    }
}
