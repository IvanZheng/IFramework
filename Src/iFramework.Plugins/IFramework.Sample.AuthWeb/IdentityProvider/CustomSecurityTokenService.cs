using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.SingleSignOn.IdentityProvider;
using System.IdentityModel.Configuration;
using IFramework.Infrastructure;
using Microsoft.AspNet.Identity.EntityFramework;
using IFramework.Sample.AuthWeb.Models;

namespace IFramework.Sample.AuthWeb.IdentityProvider
{

    public class CustomSecurityTokenService : IFramework.SingleSignOn.IdentityProvider.CustomSecurityTokenService
    {

        ApplicationUserManager _applicationUserManager;
        public CustomSecurityTokenService(SecurityTokenServiceConfiguration config)
           : base(config)
        {
            _applicationUserManager = new ApplicationUserManager(new UserStore<ApplicationUser>(new ApplicationDbContext()));
        }

        protected override ICustomIdentityObject GetCustomIdentity(string identity)
        {
            return _applicationUserManager.FindByNameAsync(identity).Result;
        }
    }
}
