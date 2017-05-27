using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using IFramework.Config;
using IFramework.Sample.AuthWeb.IdentityProvider;

namespace IFramework.Sample.AuthWeb
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Configuration.Instance.SetCustomSecurityTokenServiceType(typeof(CustomSecurityTokenService));
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}