using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.SingleSignOn.IdentityProvider;
using System.IdentityModel.Configuration;

namespace IFramework.Sample.AuthWeb.IdentityProvider
{
    public class CustomSecurityTokenService : IFramework.SingleSignOn.IdentityProvider.CustomSecurityTokenService
    {
        public CustomSecurityTokenService(SecurityTokenServiceConfiguration config)
           : base(config)
        {
        }

        protected override CustomIdentityObject GetCustomIdentity(string identity)
        {
            return new CustomIdentityObject { Identity = identity, Name = DateTime.Now.ToString() };
        }
    }
}
