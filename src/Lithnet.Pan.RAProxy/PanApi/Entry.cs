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

        public static bool operator ==(Entry e1, Entry e2)
        {
            if (Object.ReferenceEquals(e1, e2))
            {
                return true;
            }

            if (Object.ReferenceEquals(e1, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(e2, null))
            {
                return false;
            }

            return e1.Equals(e2);
        } 

        public static bool operator !=(Entry e1, Entry e2)
        {
            return !(e1 == e2);
        }

        public override bool Equals(object obj)
        {
            // Is null?
            if (Object.ReferenceEquals(null, obj))
            {
                return false;
            }

            // Is the same object?
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            Entry e = obj as Entry;

            return e != null && e.Username == this.Username && e.IpAddress == this.IpAddress;
        }
    }
}

