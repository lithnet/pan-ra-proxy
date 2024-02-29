using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    public class RadiusClientSection : ConfigurationElement
    {
        [ConfigurationProperty("accounting-port", IsRequired = false, DefaultValue = 1813)]
        public int Port
        {
            get
            {
                return (int)this["accounting-port"];
            }
            set
            {
                this["accounting-port"] = value;
            }
        }
    }
}