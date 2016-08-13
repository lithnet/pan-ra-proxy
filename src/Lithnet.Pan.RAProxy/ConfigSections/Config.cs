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
        private static RAProxyConfigurationSection section = ConfigurationManager.GetSection(RAProxyConfigurationSection.SectionName) as RAProxyConfigurationSection;

        private static Dictionary<string, string> cachedSecrets = new Dictionary<string, string>();

        public static Uri BaseUri => Config.section.PanApi.ApiUri;

        public static string ApiKey => Config.section.PanApi.ApiKey;

        public static bool DisableCertificateValidation => Config.section.PanApi.DisableCertificateValidation;

        public static int AccountingPort => Config.section.RadiusClient.Port;

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
