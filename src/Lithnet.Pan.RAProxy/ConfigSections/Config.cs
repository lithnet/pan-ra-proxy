using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Lithnet.Pan.RAProxy
{
    internal static class Config
    {
        private static IList<PanApiEndpoint> apiEndpoints;

        private static IEnumerator<PanApiEndpoint> activeEndpointEnumerator;

        private static readonly RAProxyConfigurationSection section = ConfigurationManager.GetSection(RAProxyConfigurationSection.SectionName) as RAProxyConfigurationSection;

        private static readonly Dictionary<string, string> cachedSecrets = new Dictionary<string, string>();

        private static Regex usernameRegex;

        private static bool compiledRegex;

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

            Logging.WriteEntry($"Failed over to API endpoint {Config.ActiveEndPoint.ApiUri}\n", EventLogEntryType.Warning, Logging.EventIDApiEndpointFailover);
        }

        public static bool DisableCertificateValidation => Config.section.PanApi.DisableCertificateValidation;

        public static int AccountingPort => Config.section.RadiusClient.Port;

        public static bool DebuggingEnabled => Config.section.DebuggingEnabled;

        public static int BatchSize => Config.section.PanApi.BatchSize;

        public static int BatchWait => Config.section.PanApi.BatchWait;

        private static Regex UsernameFilterRegex
        {
            get
            {
                if (!Config.compiledRegex)
                {
                    if (!string.IsNullOrEmpty(Config.section.UsernameFilter))
                    {
                        Config.usernameRegex = new Regex(Config.section.UsernameFilter, RegexOptions.IgnoreCase);
                    }

                    Config.compiledRegex = true;
                }

                return Config.usernameRegex;
            }
        }

        public static bool IsUsernameFilterMatch(string username)
        {
            if (Config.UsernameFilterRegex != null)
            {
                return Config.UsernameFilterRegex.IsMatch(username);
            }

            return false;
        }

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

            Logging.WriteEntry($"A RADIUS message was received from an unknown source {address} and was discarded", EventLogEntryType.Error, Logging.EventIDUnknownRadiusHost);

            return null;
        }

        internal static string MatchReplace(string username)
        {
            string newUsername = username;

            if (Config.section.UsernameRewrites != null)
            {
                foreach (UsernameRewriteSection rule in Config.section.UsernameRewrites)
                {
                    newUsername = rule.Rewrite(newUsername);
                }
            }

            return newUsername;
        }

        internal static string UsernameTranslationType => Config.section.UsernameRewrites.OutputFormat;
    }
}
