using System.Configuration;

namespace Lithnet.Pan.RAProxy
{
    public class UsernameRewriteCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        protected override ConfigurationElement CreateNewElement()
        {
            return new UsernameRewriteSection();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((UsernameRewriteSection)element).Key;
        }

        public UsernameRewriteSection this[int index]
        {
            get
            {
                return (UsernameRewriteSection)this.BaseGet(index);
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

        public new UsernameRewriteSection this[string name] => (UsernameRewriteSection)this.BaseGet(name);

        public int IndexOf(UsernameRewriteSection details)
        {
            return this.BaseIndexOf(details);
        }

        public void Add(UsernameRewriteSection details)
        {
            this.BaseAdd(details);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            this.BaseAdd(element, false);
        }

        public void Remove(UsernameRewriteSection details)
        {
            if (this.BaseIndexOf(details) >= 0)
                this.BaseRemove(details.Key);
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

        [ConfigurationProperty("output-format", IsRequired = false)]
        public string OutputFormat
        {
            get => (string)base["output-format"];
            set => base["output-format"] = value;
        }

        protected override string ElementName => "username-rewrite";
    }
}