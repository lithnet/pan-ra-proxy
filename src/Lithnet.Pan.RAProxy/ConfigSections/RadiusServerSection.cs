using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;

namespace Lithnet.Pan.RAProxy
{
    public class RadiusServerSection : ConfigurationElement
    {
        [ConfigurationProperty("host", IsRequired = true)]
        public string Hostname
        {
            get
            {
                return (string)this["host"];
            }
            set
            {
                this["host"] = value;
            }
        }

        [ConfigurationProperty("secret", IsRequired = true)]
        public string Secret
        {
            get
            {
                return (string)this["secret"];
            }
            set
            {
                this["secret"] = value;
            }
        }

        internal List<IPAddress> GetIpAddresses()
        {
            List<IPAddress> addresses = new List<IPAddress>();

            if (IPAddress.TryParse(this.Hostname, out IPAddress ip))
            {
                addresses.Add(ip);
            }
            else
            {
                try
                {
                    addresses.AddRange(Dns.GetHostAddresses(this.Hostname));
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.HostNotFound)
                    {
                        throw;
                    }
                }
            }

            return addresses;
        }
    }
}