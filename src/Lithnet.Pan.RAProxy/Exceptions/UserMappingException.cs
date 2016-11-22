using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Pan.RAProxy
{
    public class UserMappingException : PanApiException
    {
        public string Username { get; private set; }

        public string IPAddress { get; private set; }

        public UserMappingException(string message, string username, string ipaddress)
            :base (message)
        {
            this.Username = username;
            this.IPAddress = ipaddress;
        }
    }
}
