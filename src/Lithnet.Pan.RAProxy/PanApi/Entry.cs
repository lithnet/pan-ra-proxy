using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Pan.RAProxy
{
    using System.Xml.Serialization;

    public class Entry
    {
        [XmlAttribute(AttributeName = "name")]
        public string Username { get; set; }

        [XmlAttribute(AttributeName = "ip")]
        public string IpAddress { get; set; }

        [XmlAttribute(AttributeName = "timeout")]
        public string Timeout { get; set; }
    }
}

