using System;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IFramework.AspNet
{
    public class ApiResultWrapAttribute : ActionFilterAttribute, IApiResultWrapAttribute
    {
        public const string ServerInternalError = nameof(ServerInternalError);

        public virtual Exception OnException(Exception ex)
        {
            return ex;
        }
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            if (context.Exception != null)
            {
                var hostEnvironment = context.HttpContext
                                              .RequestServices
                                              .GetService<IHostingEnvironment>();
                var logger = context.HttpContext
                                    .RequestServices
                                    .GetService<ILoggerFactory>()
                                    .CreateLogger(context.Controller
                                                         .GetType());
                var ex = OnException(context.Exception);

                if (!(ex is HttpException))
                {
                    ApiResult exceptionResult;
                    if (ex is DomainException domainException)
                    {
                        exceptionResult = new ApiResult(domainException.ErrorCode, domainException.Message);
                        logger.LogWarning(ex, $"action failed due to domain exception");
                    }
                    else
                    {
                        exceptionResult = hostEnvironment.IsDevelopment() ? new ApiResult(ErrorCode.UnknownError, $"Message: {ex.GetBaseException().Message} StackTrace:{ex.GetBaseException().StackTrace}") : new ApiResult(ErrorCode.UnknownError, ServerInternalError);
                        logger.LogError(ex, $"action failed due to exception");
                    }
                    context.Result = new JsonResult(exceptionResult);
                    context.Exception = null;
                }
                else
                {
                    logger.LogError(ex);
                }
            }
            else
            {
                var actionResult = GetValue(context.Result);
                if (actionResult == null)
                {
                    context.Result = new JsonResult(new ApiResult());
                }
                else
                {
                    var resultType = typeof(ApiResult<>).MakeGenericType(actionResult.GetType());
                    context.Result = new JsonResult(Activator.CreateInstance(resultType, actionResult));
                }
            }
        }


        public object GetValue(IActionResult actionResult)
        {
            return (actionResult as JsonResult)?.Value ?? (actionResult as ObjectResult)?.Value;
        }
    }
}