using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace Lithnet.Pan.RAProxy.RadiusAccounting
{
    public class AccountingRequest
    {
        public IPAddress IPAddress { get;  }

        internal IReadOnlyList<RadiusAttribute> Attributes { get; }

        internal AccountingRequest(IPAddress ipAddress, IList<RadiusAttribute> attributes)
        {
            this.IPAddress = ipAddress;
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
            RadiusAttribute accountingType = this.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.AcctStatusType);
            RadiusAttribute username = this.Attributes.FirstOrDefault(t => t.Type == RadiusAttribute.RadiusAttributeType.UserName);

            if (accountingType == null)
            {
                throw new MissingValueException("The Acct-Status-Type attribute was not present");
            }

            if (username == null)
            {
                throw new MissingValueException("The Username attribute was not present");
            }
        }
    }
}
