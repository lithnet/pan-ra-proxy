using System;
using System.Collections.Generic;

namespace Lithnet.Pan.RAProxy
{
    using System.Xml.Serialization;

    public class Entry : IEquatable<Entry>
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

        public bool Equals(Entry other)
        {
            return !(other is null) &&
                   this.Username == other.Username &&
                   this.IpAddress == other.IpAddress &&
                   this.Timeout == other.Timeout;
        }

        public override int GetHashCode()
        {
            int hashCode = -1884528195;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Username);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.IpAddress);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Timeout);
            return hashCode;
        }
    }
}