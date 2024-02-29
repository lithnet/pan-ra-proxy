using System.Collections.Generic;
using System.Xml.Serialization;

namespace Lithnet.Pan.RAProxy
{
    [XmlType(TypeName = "login")]
    public class Login
    {
        public Login()
        {
            this.Entries = new List<Entry>();
        }

        [XmlElement(ElementName = "entry")]
        public List<Entry> Entries { get; set; }
    }
}
