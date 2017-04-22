using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

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

    public static class HttpConfigurationExtension
    {
        internal static IPRestrictConfig IPRestrictConfig = new IPRestrictConfig();
        public static HttpConfiguration EnableIPRestrict(this HttpConfiguration config, string configFile = null)
        {
            configFile = configFile ?? "IPRestrict.config";
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
            return config;
        }
    }
}
