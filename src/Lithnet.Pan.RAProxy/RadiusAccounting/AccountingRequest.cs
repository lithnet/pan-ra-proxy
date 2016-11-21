using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Pan.RAProxy.RadiusAccounting
{
    using System.Collections.ObjectModel;
    using System.Net;

    public class AccountingRequest
    {
        public IPAddress IPAddress { get; private set; }

        internal IReadOnlyList<RadiusAttribute> Attributes { get; private set; }

        internal AccountingRequest(IPAddress ipaddress, IList<RadiusAttribute> attributes)
        {
            this.IPAddress = ipaddress;
            this.Attributes = new ReadOnlyCollection<RadiusAttribute>(attributes);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"Source IP: {this.IPAddress}\nAttributes:\n{string.Join("\n", this.Attributes.Select(t => t.ToString()))}";
        }

        public void Validate()
        {
            RadiusAttribute accountingType = Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.AcctStatusType);
            RadiusAttribute framedIP = Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.FramedIPAddress);
            RadiusAttribute username = Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.UserName);

            if (accountingType == null)
            {
                throw new MissingValueException("The Acct-Status-Type attribute was not present");
            }

            if (framedIP == null)
            {
                throw new MissingValueException("The Framed-IP-Address attribute was not present");
            }

            if (username == null)
            {
                throw new MissingValueException("The Username attribute was not present");
            }
        }
    }
}
