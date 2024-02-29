using System.Xml;
using System.Xml.Serialization;

namespace Lithnet.Pan.RAProxy
{
    [XmlType(TypeName = "uid-message")]
    public class UidMessage : Message
    {
        public UidMessage()
        {
            this.Type = "update";
            this.Version = "1.0";
        }

        [XmlElement(ElementName = "payload")]
        public Payload Payload { get; set; }

        [XmlElement(ElementName = "type")]

        public string Type { get; set; }

        [XmlElement(ElementName = "version")]
        public string Version { get; set; }

        public override string ApiType => "user-id";
    }
}

/*
 
      <uid-message>        
        <payload>           
            <login>          
                <entry name="acme\jparker" ip="10.1.1.23" blockstart="20100">    
            </login>   
        </payload>   
      <type>update</type> 
      <version>1.0</version> </uid-message>   
     */
