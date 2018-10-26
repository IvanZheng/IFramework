using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using IFramework.AspNet;
using IFramework.Config;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Sample.CommandServiceCore.Models;

namespace Sample.CommandServiceCore.ExceptionHandlers
{
     public class GlobalExceptionHandlerOptions : ExceptionHandlerOptions
    {
        private readonly bool _isDevelopment;
        private readonly ILogger _logger;

        public GlobalExceptionHandlerOptions(ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            _logger = loggerFactory.CreateLogger<GlobalExceptionHandlerOptions>();
            _isDevelopment = env.IsDevelopment();
            ExceptionHandler = Handle;
        }

        public async Task Handle(HttpContext context)
        {
            var exception = context.Features.Get<IExceptionHandlerPathFeature>().Error;
            if (exception == null)
            {
                return;
            }
            var httpCode = (int) HttpStatusCode.InternalServerError;
            var httpException = exception as HttpException;
            var message = _isDevelopment ? exception.Message : "系统异常";
            if (httpException != null)
            {
                httpCode = httpException.StatusCode;
                message = httpException.Message;
            }
            var statusCode = (int) ErrorCode.UnknownError;
            exception.Data["HttpContext"] = context;
            if (httpCode != (int) HttpStatusCode.MethodNotAllowed && httpCode != (int) HttpStatusCode.Unauthorized)
            {
                httpCode = (int) HttpStatusCode.InternalServerError;
                _logger.LogError(null, exception);
            }
            context.Response.StatusCode = httpCode;

            ActionResult actionResult;
            if (context.Request.IsAjaxRequest())
            {
                actionResult = new JsonResult(new ApiResult
                {
                    Message = message,
                    ErrorCode = statusCode,
                    Success = false
                });
            }
            else
            {
                actionResult = new ViewResult
                {
                    ViewName = string.Format("~/Views/Shared/Error.cshtml"),
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(),
                                                      new ModelStateDictionary())
                    {
                        Model = new ErrorViewModel
                        {
                            Exception = exception
                        }
                    }
                };
            }
            await actionResult.ExecuteResultAsync(new ActionContext(context,
                                                                    context.GetRouteData(),
                                                                    new ActionDescriptor()));
        }
    }
}
