using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace Sample.CommandServiceCore.ExceptionHandlers
{
    public static class AppExceptionHandler
    {
        private static readonly ILogger Logger = IoCFactory.Resolve<ILoggerFactory>()
                                                  .CreateLogger(nameof(AppExceptionHandler));

        public static async Task Handle(HttpContext context)
        {
            context.Response.ContentType = "text/html";
            Exception exception = context.Features.Get<IExceptionHandlerPathFeature>().Error;
            if (exception == null)
            {
                return;
            }
            AggregateException aggregateEx;
            while ((aggregateEx = exception as AggregateException) != null)
            {
                exception = aggregateEx.InnerException;
            }

            var request = context.Request;
            var requestBody = string.Empty;
            if (request.Method != HttpMethod.Get.Method)
            {
                using (var stream = new MemoryStream())
                {
                    context.Request.Body.Seek(0, SeekOrigin.Begin);
                    context.Request.Body.CopyTo(stream);
                    requestBody = Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            Logger.LogError(exception, requestBody);

            await context.Response.WriteAsync(new ApiResult(500, exception.Message).ToJson());
        }

    }
}
