using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    public class PanApiSection : ConfigurationElement
    {
        private Uri uri;

        [ConfigurationProperty("url", IsRequired = true)]
        public Uri ApiUri
        {
            get
            {
                if (this.uri == null)
                {
                    string uristring = (string)this["url"];

                    if (uristring != null)
                    {
                        this.uri = new Uri(uristring);
                    }
                }

                return this.uri;
            }

            set
            {
                this["url"] = value?.ToString();
            }
        }

        [ConfigurationProperty("api-key", IsRequired = true)]
        public string ApiKey
        {
            get
            {
                return (string)this["api-key"];
            }

            set
            {
                this["api-key"] = value;
            }
        }


        [ConfigurationProperty("disable-certificate-validation", IsRequired = false, DefaultValue = false)]
        public bool DisableCertificateValidation
        {
            get
            {
                return (bool)this["disable-certificate-validation"];
            }

            set
            {
                this["disable-certificate-validation"] = value;
            }
        }
    }
}