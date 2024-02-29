using System.Xml.Serialization;

namespace Lithnet.Pan.RAProxy
{

    [XmlType(TypeName = "payload")]
    public class Payload
    {
        public Payload()
        {
            this.Login = new Login();
            this.Logout = new Logout();
        }

        [XmlElement(ElementName = "login")]
        public Login Login { get; set; }

        [XmlElement(ElementName = "logout")]
        public Logout Logout { get; set; }
    }
}
