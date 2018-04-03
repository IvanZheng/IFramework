using System;
using System.Collections.Generic;
using System.Text;

#if !Legency
using Microsoft.AspNetCore.Http;
#endif

namespace IFramework.AspNet
{
#if !Legency
    public static class CookieExtension
    {
        /// <summary>
        /// 取Cookie值
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetCookieValue(this HttpContext httpContext, string name)
        {
            return httpContext.Request.Cookies[name] ?? string.Empty;
        }

        /// <summary>
        /// 移除Cookie
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="name"></param>
        public static void RemoveCookie(this HttpContext httpContext, string name)
        {
            httpContext.Response.Cookies.Delete(name);
        }


        public static string GetServerDomain(this HttpRequest request)
        {
            var urlHost = request.GetUri().Host.ToLower();
            var urlHostArray = urlHost.Split('.');
            if ((urlHostArray.Length < 3) || RegExp.IsIp(urlHost))
            {
                return urlHost;
            }
            var urlHost2 = urlHost.Remove(0, urlHost.IndexOf(".", StringComparison.Ordinal) + 1);
            if ((urlHost2.StartsWith("com.") || urlHost2.StartsWith("net.")) || (urlHost2.StartsWith("org.") || urlHost2.StartsWith("gov.")))
            {
                return urlHost;
            }
            return urlHost2;
        }

        public static void SaveCookie(this HttpContext httpContext, string name, string value, int expiresHours = 0)
        {
            CookieOptions option = new CookieOptions();
            string domain = httpContext.Request.GetServerDomain();
            string urlHost = httpContext.Request.GetUri().Host.ToLower();
            if (domain != urlHost)
                option.Domain = domain;

            if (expiresHours > 0)
                option.Expires = DateTime.Now.AddHours(expiresHours);

            httpContext.Response.Cookies.Append(name, value, option);
        }
    }
#endif
}
