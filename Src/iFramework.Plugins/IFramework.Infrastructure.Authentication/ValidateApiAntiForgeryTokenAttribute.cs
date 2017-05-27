using System.Linq;
using System.Net.Http;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace IFramework.Infrastructure.WebAuthentication
{
    public class ValidateApiAntiForgeryTokenAttribute : AuthorizeAttribute
    {
        private readonly bool _authorize;

        public ValidateApiAntiForgeryTokenAttribute(bool authorize = false)
        {
            _authorize = authorize;
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            try
            {
                var headerToken = actionContext
                    .Request
                    .Headers
                    .GetValues("__RequestVerificationToken")
                    .FirstOrDefault();
                ;

                var cookieToken = actionContext
                    .Request
                    .Headers
                    .GetCookies()
                    .Select(c => c[AntiForgeryConfig.CookieName])
                    .FirstOrDefault();

                // check for missing cookie or header
                if (cookieToken == null || headerToken == null)
                    return false;

                // ensure that the cookie matches the header

                AntiForgery.Validate(cookieToken.Value, headerToken);
            }
            catch
            {
                return false;
            }


            return !_authorize || base.IsAuthorized(actionContext);
        }
    }
}