using System.IdentityModel.Services;
using System.Security.Claims;
using System.Web.Mvc;
using IFramework.SingleSignOn.IdentityProvider;

namespace IFramework.Sample.AuthWeb.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Index()
        {
            FederatedPassiveSecurityTokenServiceOperations.ProcessRequest(
                System.Web.HttpContext.Current.Request,
                User as ClaimsPrincipal,
                CustomSecurityTokenServiceConfiguration.Current.CreateSecurityTokenService(),
                System.Web.HttpContext.Current.Response);
            return View();
        }

        /*public ActionResult Index(string login)
        {
            Session["LoginPage"] = login;
            return RedirectToAction("Redirect");
        }*/

        [Authorize]
        public ActionResult Redirect()
        {
            FederatedPassiveSecurityTokenServiceOperations.ProcessRequest(
                System.Web.HttpContext.Current.Request,
                User as ClaimsPrincipal,
                CustomSecurityTokenServiceConfiguration.Current.CreateSecurityTokenService(),
                System.Web.HttpContext.Current.Response);
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}