using System.Web.Mvc;
using System.Web.Routing;

namespace Sample.CommandService
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{service}.svc/{*pathInfo}");
            routes.MapRoute(
                            "Default",
                            "{controller}/{action}/{id}",
                            new {controller = "Test", action = "Index", id = UrlParameter.Optional}
                           );
        }
    }
}