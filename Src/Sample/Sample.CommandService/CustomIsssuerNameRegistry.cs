using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Sample.CommandService
{
    public class CustomIsssuerNameRegistry : ConfigurationBasedIssuerNameRegistry
    {
        public override string GetIssuerName(System.IdentityModel.Tokens.SecurityToken securityToken)
        {
            if (securityToken == null)
            {
                throw new Exception("securityToken");
            }
            X509SecurityToken token = securityToken as X509SecurityToken;
            if (token != null)
            {
                string thumbprint = token.Certificate.Thumbprint;
                if (this.ConfiguredTrustedIssuers.ContainsKey(thumbprint)) //Breakpoint here
                {
                    return this.ConfiguredTrustedIssuers[thumbprint];
                }
            }
            return null;
            //var ret = base.GetIssuerName(securityToken);
            //return ret;
        }

        public override string GetIssuerName(System.IdentityModel.Tokens.SecurityToken securityToken, string requestedIssuerName)
        {
            var ret = base.GetIssuerName(securityToken, requestedIssuerName);

            return ret;
        }
    }
}
