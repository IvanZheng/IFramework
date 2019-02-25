using System.Collections.Generic;

namespace IFramework.AspNet
{
    public class IpFilterOption
    {
        public bool Enabled { get; set; }
        public IpFilterOption()
        {
            GlobalWhiteList = new List<string>();
            EntryWhiteListDictionary = new Dictionary<string, List<string>>();
        }

        public List<string> GlobalWhiteList { get; set; }

        public Dictionary<string, List<string>> EntryWhiteListDictionary { get; set; }
    }
}