using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    public class PanApiCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        protected override ConfigurationElement CreateNewElement()
        {
            return new PanApiEndpoint();
        }

        [ConfigurationProperty("disable-certificate-validation", IsRequired = false, DefaultValue = false)]
        public bool DisableCertificateValidation
        {
            get
            {
                return (bool)base["disable-certificate-validation"];
            }

            set
            {
                base["disable-certificate-validation"] = value;
            }
        }

        [ConfigurationProperty("batch-size", IsRequired = false, DefaultValue = 200)]
        public int BatchSize
        {
            get
            {
                return (int)base["batch-size"];
            }

            set
            {
                base["batch-size"] = value;
            }
        }

        [ConfigurationProperty("batch-wait", IsRequired = false, DefaultValue = 50)]
        public int BatchWait
        {
            get
            {
                return (int)base["batch-wait"];
            }

            set
            {
                base["batch-wait"] = value;
            }
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PanApiEndpoint)element).ApiUri;
        }

        public PanApiEndpoint this[int index]
        {
            get
            {
                return (PanApiEndpoint)this.BaseGet(index);
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

        public new PanApiEndpoint this[string name] => (PanApiEndpoint)this.BaseGet(name);

        public int IndexOf(PanApiEndpoint details)
        {
            return this.BaseIndexOf(details);
        }

        public void Add(PanApiEndpoint details)
        {
            this.BaseAdd(details);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            this.BaseAdd(element, false);
        }

        public void Remove(PanApiEndpoint details)
        {
            if (this.BaseIndexOf(details) >= 0)
            {
                this.BaseRemove(details.ApiUri);
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

        protected override string ElementName => "pan-api-endpoint";
    }
}