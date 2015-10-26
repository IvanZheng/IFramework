using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace IFramework.WebApi
{
    public static class ControllerExtension
    {
        public static string TryGetCookie(this Controller controller, string key, string defaultValue)
        {
            var cookieValue = defaultValue;
            var cookie = controller.Request.Cookies[key];
            if (cookie != null)
            {
                cookieValue = cookie.Value;
            }
            return cookieValue;
        }
    }
}
