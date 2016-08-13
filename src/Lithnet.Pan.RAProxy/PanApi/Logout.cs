using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lithnet.Pan.RAProxy
{
    [XmlType(TypeName = "logout")]
    public class Logout
    {
        public Logout()
        {
            this.Entries = new List<Entry>();
        }

        [XmlElement(ElementName = "entry")]
        public List<Entry> Entries { get; set; }
    }
}
