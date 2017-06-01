using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Http;
using IFramework.Infrastructure;

namespace IFramework.AspNet
{
    public class IPRestrictConfig
    {
        public IPRestrictConfig()
        {
            GlobalWhiteList = new List<string>();
            EntryWhiteListDictionary = new Dictionary<string, List<string>>();
        }

        public List<string> GlobalWhiteList { get; set; }
        public Dictionary<string, List<string>> EntryWhiteListDictionary { get; set; }
    }

    public static class IPRestrictExtension
    {
        internal static bool Enabled;
        internal static IPRestrictConfig IPRestrictConfig = new IPRestrictConfig();

        public static HttpConfiguration EnableIPRestrict(this HttpConfiguration config, string configFile = null)
        {
            configFile = configFile ?? "ipRestrict.json";
            var file = new FileInfo(configFile);
            if (!file.Exists)
            {
                file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFile));
            }

            if (file.Exists)
            {
                var json = File.ReadAllText(file.FullName);
                IPRestrictConfig = json.ToJsonObject<IPRestrictConfig>();
            }
            Enabled = true;
            return config;
        }
    }
}