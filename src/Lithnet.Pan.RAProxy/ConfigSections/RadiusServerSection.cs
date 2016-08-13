using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    using System.Net;
    using System.Net.Sockets;

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

            IPAddress ip;

            if (IPAddress.TryParse(this.Hostname, out ip))
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