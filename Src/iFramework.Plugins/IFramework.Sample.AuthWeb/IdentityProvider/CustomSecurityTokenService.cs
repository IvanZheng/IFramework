using System.IdentityModel.Configuration;
using IFramework.Sample.AuthWeb.Models;
using IFramework.SingleSignOn.IdentityProvider;
using Microsoft.AspNet.Identity.EntityFramework;

namespace IFramework.Sample.AuthWeb.IdentityProvider
{
    public class CustomSecurityTokenService : SingleSignOn.IdentityProvider.CustomSecurityTokenService
    {
        private readonly ApplicationUserManager _applicationUserManager;

        public CustomSecurityTokenService(SecurityTokenServiceConfiguration config)
            : base(config)
        {
            _applicationUserManager =
                new ApplicationUserManager(new UserStore<ApplicationUser>(new ApplicationDbContext()));
        }

        protected override ICustomIdentityObject GetCustomIdentity(string identity)
        {
            return _applicationUserManager.FindByNameAsync(identity).Result;
        }
    }
}