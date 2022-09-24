﻿using System;
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
#if !NET5_0_OR_GREATER
using Microsoft.AspNetCore.Http.Internal;
#endif
#endif
namespace IFramework.AspNet
{
    #if !Legency
    public static partial class Extensions
    {
        public static string FormatUri(this string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var validatedUri);
            if (validatedUri.IsAbsoluteUri)
            {
                return validatedUri.AbsoluteUri;
            }

            var uri = new Uri(new Uri("http://127.0.0.1"), validatedUri.ToString());
            if (!url.StartsWith("/"))
            {
                return uri.PathAndQuery.Substring(1);
            }

            return uri.PathAndQuery;
        }

        public static HttpResponse EnableRewind(this HttpResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            var body = response.Body;
            if (!body.CanSeek)
            {
                var responseBodyStream = new WriteSyncMemoryStream(body);
                response.Body = responseBodyStream;
                response.HttpContext.Response.RegisterForDispose(responseBodyStream);
            }
            return response;
        }

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Cookies["X-Requested-With"] == "XMLHttpRequest")
                return true;
            if (request.Headers != null)
                return request.Headers["X-Requested-With"] == "XMLHttpRequest";
            return false;
        }

        public static string GetClientIp(this HttpRequest me)
        {
            string ip = null;

            // todo support new "Forwarded" header (2014) https://en.wikipedia.org/wiki/X-Forwarded-For

            // X-Forwarded-For (csv list):  Using the First entry in the list seems to work
            // for 99% of cases however it has been suggested that a better (although tedious)
            // approach might be to read each IP from right to left and use the first public IP.
            // http://stackoverflow.com/a/43554000/538763
            //
            ip = GetHeaderValueAs<string>(me.HttpContext, "X-Forwarded-For").SplitCsv().FirstOrDefault();

            // RemoteIpAddress is always null in DNX RC1 Update1 (bug).
            if (ip.IsNullOrWhitespace() && me.HttpContext?.Connection?.RemoteIpAddress != null)
            {
                ip = me.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            if (ip.IsNullOrWhitespace())
            {
                ip = GetHeaderValueAs<string>(me.HttpContext, "REMOTE_ADDR");
            }

            // _httpContextAccessor.HttpContext?.Request?.Host this is the local host.

            if (ip.IsNullOrWhitespace())
            {
                throw new Exception("Unable to determine caller's IP.");
            }

            return ip;
        }

        public static T GetHeaderValueAs<T>(HttpContext httpContext, string headerName)
        {
            if (httpContext.Request.Headers.TryGetValue(headerName, out var values))
            {
                var rawValues = values.ToString(); // writes out as Csv when there are multiple.

                if (!rawValues.IsNullOrWhitespace())
                {
                    return (T) Convert.ChangeType(values.ToString(), typeof(T));
                }
            }
            
            return default(T);
        }

        public static List<string> SplitCsv(this string csvList, bool nullOrWhitespaceInputReturnsNull = false)
        {
            if (string.IsNullOrWhiteSpace(csvList))
            {
                return nullOrWhitespaceInputReturnsNull ? null : new List<string>();
            }

            return csvList.TrimEnd(',')
                          .Split(',')
                          .Select(s => s.Trim())
                          .ToList();
        }

        public static bool IsNullOrWhitespace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static IApplicationBuilder UseMessageProcessorDashboardMiddleware(this IApplicationBuilder app, string path = "/api/messageProcessors")
        {
            app.Map(PathString.FromUriComponent(path),
                    configuration => { configuration.UseMiddleware<MessageProcessorDashboardMiddleware>(); });
            return app;
        }

        public static IApplicationBuilder UseLogLevelController(this IApplicationBuilder app, string path = "/api/logLevels")
        {
            app.Map(PathString.FromUriComponent(path), 
                    configuration => { configuration.UseMiddleware<LogLevelControllerMiddleware>(); });
            return app;
        }

        public static IApplicationBuilder UseEnableRewind(this IApplicationBuilder app)
        {
            app.Use(next => context => {
                context.Request.EnableBuffering();
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
            
            return null;
        }

        public static bool TryGetFormValue(this HttpRequest request, string key, out StringValues value)
        {
            value = default;
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
            var oriPosition = request.Body.Position;
            string body = null;
            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
                var streamReader = new StreamReader(request.Body);
                request.HttpContext.Response.RegisterForDispose(streamReader);
                body = streamReader.ReadToEnd();
                request.Body.Seek(oriPosition, SeekOrigin.Begin);
            }
            return body;
        }

        public static async Task<string> GetRequestBodyStringAsync(this HttpRequest request)
        {
            var oriposition = request.Body.Position;
            string body = null;
            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
                var streamReader = new StreamReader(request.Body);
                request.HttpContext.Response.RegisterForDispose(streamReader);
                body = await streamReader.ReadToEndAsync();
                request.Body.Seek(oriposition, SeekOrigin.Begin);
            }
            return body;
        }



        //public static string GetRequestContent(this HttpContext me, [CallerMemberName] string tag = null)
        //{
        //    var request = me.Request;
        //    using (var stream = new MemoryStream())
        //    {
        //        me.Request.Body.Seek(0, SeekOrigin.Begin);
        //        me.Request.Body.CopyTo(stream);
        //        var requestBody = Encoding.UTF8.GetString(stream.ToArray());
        //        return requestBody;
        //    }
        //}

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

