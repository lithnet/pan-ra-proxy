using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    public class RadiusServerCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        protected override ConfigurationElement CreateNewElement()
        {
            return new RadiusServerSection();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RadiusServerSection)element).Hostname;
        }

        public RadiusServerSection this[int index]
        {
            get
            {
                return (RadiusServerSection)this.BaseGet(index);
            }
            set
            {
                if (this.BaseGet(index) != null)
                {
                    this.BaseRemoveAt(index);
                }

                this.BaseAdd(index, value);
            }
        }

        public new RadiusServerSection this[string name] => (RadiusServerSection)this.BaseGet(name);

        public int IndexOf(RadiusServerSection details)
        {
            return this.BaseIndexOf(details);
        }

        public void Add(RadiusServerSection details)
        {
            this.BaseAdd(details);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            this.BaseAdd(element, false);
        }

        public void Remove(RadiusServerSection details)
        {
            if (this.BaseIndexOf(details) >= 0)
            {
                this.BaseRemove(details.Hostname);
            }
        }

        public void RemoveAt(int index)
        {
            this.BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            this.BaseRemove(name);
        }

        public void Clear()
        {
            this.BaseClear();
        }

        protected override string ElementName => "radius-server";
    }
}