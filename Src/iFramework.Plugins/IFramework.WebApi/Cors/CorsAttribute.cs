using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace IFramework.AspNet.Cors
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class CorsAttribute : Attribute
    {
        static ILogger _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(CorsAttribute));
        
        static CorsAttribute()
        {
            try
            {
                AllowOrigins = Configuration.GetAppConfig("AllowCorsOrigins")
                                                  .Split(new char[] { ',' },
                                                        StringSplitOptions.RemoveEmptyEntries);
                
                _Logger.Debug(AllowOrigins.ToJson());
            }
            catch (Exception ex)
            {
                _Logger.Error(Configuration.GetAppConfig("AllowCorsOrigins"), ex);
            }
        }
        public static string[] AllowOrigins
        {
            get;
            private set;
        }
        public string ErrorMessage { get; private set; }
        public CorsAttribute()
        {

        }
        public bool TryEvaluate(HttpRequestMessage request, out IDictionary<string, string> headers)
        {
            headers = null;
            string origin = null;
            try
            {
                origin = request.Headers.GetValues("Origin").FirstOrDefault();
            }
            catch (Exception)
            {
                this.ErrorMessage = "Cross-origin request denied";
                return false;
            }
            Uri originUri = new Uri(origin);
            _Logger.DebugFormat("{0} origin: {1}", AllowOrigins.ToJson(), originUri.Authority);
            if (AllowOrigins.Contains(originUri.Authority))
            {
                headers = this.GenerateResponseHeaders(request);
                return true;
            }

            this.ErrorMessage = "Cross-origin request denied";
            return false;
        }

        private IDictionary<string, string> GenerateResponseHeaders(HttpRequestMessage request)
        {

            //设置响应头"Access-Control-Allow-Methods"

            string origin = request.Headers.GetValues("Origin").First();

            Dictionary<string, string> headers = new Dictionary<string, string>();

            headers.Add("Access-Control-Allow-Origin", origin);

            if (request.IsPreflightRequest())
            {
                //设置响应头"Access-Control-Request-Headers"
                //和"Access-Control-Allow-Headers"
                headers.Add("Access-Control-Allow-Methods", "*");

                string requestHeaders = request.Headers.GetValues("Access-Control-Request-Headers").FirstOrDefault();

                if (!string.IsNullOrEmpty(requestHeaders))
                {
                    headers.Add("Access-Control-Allow-Headers", requestHeaders);
                }
            }
            return headers;
        }
    }
}