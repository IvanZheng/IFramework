using System;
using System.Web.Mvc;

namespace IFramework.AspNet
{
    public static class ControllerExtension
    {
        public static string TryGetCookie(this Controller controller, string key, string defaultValue)
        {
            try
            {
                var cookieValue = defaultValue;
                var cookie = controller.Request.Cookies[key];
                if (cookie != null)
                {
                    cookieValue = cookie.Value;
                }
                return cookieValue;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}