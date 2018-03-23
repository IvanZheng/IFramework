using System;
using System.IdentityModel.Tokens;

namespace Sample.CommandService
{
    public class CustomIsssuerNameRegistry : ConfigurationBasedIssuerNameRegistry
    {
        public override string GetIssuerName(SecurityToken securityToken)
        {
            if (securityToken == null)
            {
                throw new Exception("securityToken");
            }
            var token = securityToken as X509SecurityToken;
            if (token != null)
            {
                var thumbprint = token.Certificate.Thumbprint;
                if (ConfiguredTrustedIssuers.ContainsKey(thumbprint)) //Breakpoint here
                {
                    return ConfiguredTrustedIssuers[thumbprint];
                }
            }
            return null;
            //var ret = base.GetIssuerName(securityToken);
            //return ret;
        }

        public override string GetIssuerName(SecurityToken securityToken, string requestedIssuerName)
        {
            var ret = base.GetIssuerName(securityToken, requestedIssuerName);

            return ret;
        }
    }
}