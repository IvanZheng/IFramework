using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IFramework.MessageQueue;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IFramework.AspNet
{
    public class MessageProcessorDashboardMiddleware
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly RequestDelegate _next;

        /// <summary>Creates a new instance of the StaticFileMiddleware.</summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="hostingEnv">The <see cref="T:Microsoft.AspNetCore.Hosting.IHostingEnvironment" /> used by this middleware.</param>
        /// <param name="loggerFactory"></param>
        public MessageProcessorDashboardMiddleware(RequestDelegate next, 
#if NET5
                                                   IWebHostEnvironment hostingEnv,
#else
                                                   IHostingEnvironment hostingEnv,
#endif
                                                   ILoggerFactory loggerFactory)
        {
            if (hostingEnv == null)
            {
                throw new ArgumentNullException(nameof(hostingEnv));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _loggerFactory = loggerFactory;
            _next = next;
        }

        /// <summary>
        ///     Processes a request to determine if it matches a known file, and if so, serves it.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                if (context.Request.Method.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
                {
                    var messageProcessorInfo = MessageQueueFactory.MessageProcessors
                                                                  .Select(p => p.GetStatus())
                                                                  .ToArray();
                    await context.Response.WriteAsync(string.Join("\r\n", messageProcessorInfo));
                }
                else
                {
                    await _next(context);
                }
            }
            catch (Exception e)
            {
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(e.GetBaseException().Message);
            }
        }
    }
}