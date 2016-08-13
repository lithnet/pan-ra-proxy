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
    }
}
