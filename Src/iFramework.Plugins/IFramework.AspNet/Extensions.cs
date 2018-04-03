using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
#if !Legency
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http.Internal;
#endif
namespace IFramework.AspNet
{
    #if !Legency
    public static partial class Extensions
    {
        public static IApplicationBuilder UseEnableRewind(this IApplicationBuilder app)
        {
            app.Use(next => context => {
                context.Request.EnableRewind();
                return next(context);
            });
            return app;
        }

        public static CultureInfo GetCulture(this HttpContext httpContext)
        {
            // Retrieves the requested culture
            return httpContext.Features
                              .Get<IRequestCultureFeature>()
                              .RequestCulture.Culture;
        }

        public static Uri GetReferrerUri(this HttpRequest request)
        {
            var refererUrl = request.Headers["Referer"].ToString();
            return refererUrl.ToUri();
        }

        public static Uri GetUri(this HttpRequest request)
        {
            return new Uri(request.GetDisplayUrl());
        }


        private static Uri ToUri(this string url)
        {
            return new Uri(url);
        }

        public static T GetService<T>(this IServiceProvider scope)
        {
            return (T)scope.GetService(typeof(T));
        }

        public static string Get(this HttpRequest request, string key)
        {
            if (request.Cookies.TryGetValue(key, out var cookieValue))
            {
                return cookieValue;
            }

            if (request.Query.TryGetValue(key, out var queryValue))
            {
                return queryValue;
            }

            if (request.TryGetFormValue(key, out var formValue))
            {
                return formValue;
            }

            //TODO 还需要添加从ServerVariables取值的方式。
            return null;
        }

        public static bool TryGetFormValue(this HttpRequest request, string key, out StringValues value)
        {
            return request.HasFormContentType && request.Form.TryGetValue(key, out value);
        }

        public static string GetFormValue(this HttpRequest request, string key)
        {
            if (request.HasFormContentType)
            {
                return request.Form[key];
            }
            return null;
        }

        public static string GetRequestBodyString(this HttpRequest request)
        {
            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }
            var streamReader = new StreamReader(request.Body);
            request.HttpContext.Response.RegisterForDispose(streamReader);
            return streamReader.ReadToEnd();
        }

        public static async Task<string> GetRequestBodyStringAsync(this HttpRequest request)
        {
            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }
            var streamReader = new StreamReader(request.Body);
            request.HttpContext.Response.RegisterForDispose(streamReader);
            return await streamReader.ReadToEndAsync();
        }



        public static string GetRequestContent(this HttpContext me, [CallerMemberName] string tag = null)
        {
            var request = me.Request;
            using (var stream = new MemoryStream())
            {
                me.Request.Body.Seek(0, SeekOrigin.Begin);
                me.Request.Body.CopyTo(stream);
                var requestBody = Encoding.UTF8.GetString(stream.ToArray());

                return requestBody;
            }
        }

        public static string GetClientIp(this HttpRequest me)
        {
            var ip = me.HttpContext.Connection.RemoteIpAddress;
            return ip?.ToString().Trim();
        }


        public static IDictionary<string, string> FromLegacyCookieString(this string legacyCookie)
        {
            return legacyCookie.Split('&').Select(s => s.Split('=')).ToDictionary(kvp => kvp[0], kvp => kvp[1]);
        }

        public static string ToLegacyCookieString(this IDictionary<string, string> dict)
        {
            return string.Join("&", dict.Select(kvp => string.Join("=", kvp.Key, kvp.Value)));
        }
    }
    #endif
}

