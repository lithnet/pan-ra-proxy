using System;
using System.Configuration;
using System.Text.RegularExpressions;

namespace Lithnet.Pan.RAProxy
{
    public class UsernameRewriteSection : ConfigurationElement
    {
        private Regex match;

        private string key;

        internal string Key
        {
            get
            {
                if (this.key == null)
                {
                    this.key = Guid.NewGuid().ToString();
                }

                return this.key;
            }
        }

        [ConfigurationProperty("match", IsRequired = true)]
        public string Match
        {
            get
            {
                return (string)this["match"];
            }
            set
            {
                this["match"] = value;
            }
        }

        [ConfigurationProperty("replace", IsRequired = true)]
        public string Replace
        {
            get
            {
                return (string)this["replace"];
            }
            set
            {
                this["replace"] = value;
            }
        }

        internal Regex MatchRegex
        {
            get
            {
                if (this.match == null)
                {
                    this.match = new Regex(this.Match, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }

                return this.match;
            }
        }

        internal string Rewrite(string username)
        {
            return this.MatchRegex.Replace(username, this.Replace);
        }
    }
}