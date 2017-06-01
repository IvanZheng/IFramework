using System.Web;

namespace IFramework.SingleSignOn.IdentityProvider
{
    public class SingleSignOnManager
    {
        private const string SiteCookieName = "StsSiteCookie";
        private const string SiteName = "StsSite";

        /// <summary>
        ///     Returns a list of sites the user is logged in via the STS
        /// </summary>
        /// <returns></returns>
        public static string[] SignOut()
        {
            if (HttpContext.Current != null)
            {
                var siteCookie = HttpContext.Current.Request.Cookies[SiteCookieName];

                if (siteCookie != null)
                {
                    return siteCookie.Values.GetValues(SiteName);
                }
            }

            return new string[0];
        }

        public static void RegisterRP(string siteUrl)
        {
            if (HttpContext.Current != null)
            {
                // get an existing cookie or create a new one
                var siteCookie =
                    HttpContext.Current.Request.Cookies[SiteCookieName];
                if (siteCookie == null)
                {
                    siteCookie = new HttpCookie(SiteCookieName);
                }

                siteCookie.Values.Add(SiteName, siteUrl);

                HttpContext.Current.Response.AppendCookie(siteCookie);
            }
        }
    }
}