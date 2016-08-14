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
                return (Uri)this["url"];
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