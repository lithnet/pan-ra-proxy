using System;
using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    public class PanApiEndpoint : ConfigurationElement
    {
        [ConfigurationProperty("url", IsRequired = true)]
        public Uri ApiUri
        {
            get
            {
                return (Uri) this["url"];
            }

            set
            {
                this["url"] = value;
            }
        }

        [ConfigurationProperty("api-key", IsRequired = true)]
        public string ApiKey
        {
            get
            {
                return (string) this["api-key"];
            }

            set
            {
                this["api-key"] = value;
            }
        }
    }
}