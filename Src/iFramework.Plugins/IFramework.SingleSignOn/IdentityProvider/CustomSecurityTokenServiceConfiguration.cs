using IFramework.Config;
using System.IdentityModel.Configuration;
using System.Web;
using System.Web.Configuration;

namespace IFramework.SingleSignOn.IdentityProvider
{
    public class CustomSecurityTokenServiceConfiguration : SecurityTokenServiceConfiguration
    {
        private static readonly object SyncRoot = new object();
        private const string CustomSecurityTokenServiceConfigurationKey = "CustomSecurityTokenServiceConfigurationKey";

        public CustomSecurityTokenServiceConfiguration()
            : base(WebConfigurationManager.AppSettings[Common.IssuerName])
        {
            this.SecurityTokenService = Configuration.Instance.GetCustomSecurityTokenServiceType();
        }

        public static CustomSecurityTokenServiceConfiguration Current
        {
            get
            {
                var app = HttpContext.Current.Application;
                var config = app.Get(CustomSecurityTokenServiceConfigurationKey) as CustomSecurityTokenServiceConfiguration;
                if (config != null)
                    return config;
                lock (SyncRoot)
                {
                    config = app.Get(CustomSecurityTokenServiceConfigurationKey) as CustomSecurityTokenServiceConfiguration;
                    if (config == null)
                    {
                        config = new CustomSecurityTokenServiceConfiguration();
                        app.Add(CustomSecurityTokenServiceConfigurationKey, config);
                    }
                    return config;
                }
            }
        }
        
    }

}