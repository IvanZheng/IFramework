using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.AspNet
{
    public class SetLogLevelRequest
    {
        public string Name { get; set; }
        public LogLevel MinLevel { get; set; }
    }
    public class LogLevelControllerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>Creates a new instance of the StaticFileMiddleware.</summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="hostingEnv">The <see cref="T:Microsoft.AspNetCore.Hosting.IHostingEnvironment" /> used by this middleware.</param>
        public LogLevelControllerMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, ILoggerFactory loggerFactory)
        {
            if (hostingEnv == null)
                throw new ArgumentNullException(nameof(hostingEnv));

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _loggerFactory = loggerFactory;
            _next = next;
        }

        /// <summary>
        /// Processes a request to determine if it matches a known file, and if so, serves it.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                if (context.Request.Method.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
                {
                    var loggerNamePrefix = context.Request.Query["name"];
                    var loggerInfos = _loggerFactory.GetLoggers().Select(l => l.GetInfo());
                    if (!string.IsNullOrWhiteSpace(loggerNamePrefix))
                    {
                        loggerInfos = loggerInfos.Where(i => i.Name.StartsWith(loggerNamePrefix));
                    }                     
                    await context.Response.WriteAsync(loggerInfos.ToJson());
                }
                else if (context.Request.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
                {
                    var requestBody = await context.Request.GetRequestBodyStringAsync();
                    var request = requestBody.ToJsonObject<SetLogLevelRequest>();
                    if (request == null)
                    {
                        throw new Exception("request body is null!");
                    }

                    var logger = _loggerFactory.CreateLogger(request.Name);
                    logger.SetMinLevel(request.MinLevel);
                }
            }
            catch (Exception e)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(e.GetBaseException().Message);
            }
            
        }
    }
}
