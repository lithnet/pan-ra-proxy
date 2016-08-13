using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lithnet.Pan.RAProxy
{

    [XmlType(TypeName = "payload")]
    public class Payload
    {
        public Payload()
        {
        }

        [XmlElement(ElementName = "login")]
        public Login Login { get; set; }

        [XmlElement(ElementName = "logout")]
        public Logout Logout { get; set; }
    }
}
