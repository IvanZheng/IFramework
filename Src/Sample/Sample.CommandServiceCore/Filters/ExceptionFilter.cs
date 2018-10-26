using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Sample.CommandServiceCore.Models;
using IFramework.AspNet;

namespace Sample.CommandServiceCore.Filters
{
    public class ExceptionFilter : ExceptionFilterAttribute
    {
        private readonly ILogger _logger;

        public ExceptionFilter(ILogger<ExceptionFilter> logger)
        {
            _logger = logger;
        }

        public  override async Task OnExceptionAsync(ExceptionContext context)
        {
            var exception = context.Exception;
            if (exception == null)
            {
                return;
            }

            var request = context.HttpContext.Request;
            var contentType = request.Headers["Content-Type"];
            var isAjax = request.IsAjaxRequest();
            exception = exception.GetBaseException();

            var requestBody = string.Empty;
            if (request.Method != HttpMethod.Get.Method)
            {
                requestBody = await request.GetRequestBodyStringAsync();
            }

            var requestInfo = $"Headers: {context.HttpContext.Request.Headers.ToJson()}{Environment.NewLine}RequestUri: {context.HttpContext.Request.GetDisplayUrl()}{Environment.NewLine}RequestBody: {requestBody}";
           
            _logger.LogError(exception, requestInfo);


            if (!isAjax && contentType != "application/json" && contentType != "x-www-form-urlencoded")
            {
                context.Result = new ViewResult()
                {
                    ViewName = "~/Views/Shared/Error.cshtml",
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
            else
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
#if DEBUG
                context.Result = new JsonResult(new ApiResult(ErrorCode.UnknownError, $"{exception.Message} {exception.StackTrace} {requestInfo}"));
#else
                context.Result = new JsonResult(new ApiResult(ErrorCode.UnknownError, "服务器打了个盹。"));
#endif
            }
            await base.OnExceptionAsync(context);
        }
    }
}
