using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    public class RAProxyConfigurationSection : ConfigurationSection
    {
        public const string SectionName = "ra-proxy-config";

        [ConfigurationProperty("radius-servers")]
        public RadiusServerCollection RadiusServers
        {
            get
            {
                return (RadiusServerCollection)this["radius-servers"];
            }

            set
            {
                this["radius-servers"] = value;
            }
        }

        [ConfigurationProperty("radius-client")]
        public RadiusClientSection RadiusClient
        {
            get
            {
                return (RadiusClientSection)this["radius-client"];
            }

            set
            {
                this["radius-client"] = value;
            }
        }

        [ConfigurationProperty("pan-api")]
        public PanApiSection PanApi
        {
            get
            {
                return (PanApiSection)this["pan-api"];
            }

            set
            {
                this["pan-api"] = value;
            }
        }
    }
}