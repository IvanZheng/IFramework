using System;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sample.CommandServiceCore.Filters
{
    public class ApiResultWrapAttribute : ActionFilterAttribute
    {
        public const string ServerInternalError = nameof(ServerInternalError);

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            if (context.Exception != null)
            {
                var logger = context.HttpContext
                                    .RequestServices
                                    .GetService<ILoggerFactory>()
                                    .CreateLogger(context.Controller
                                                         .GetType()
                                                         .Name);
                ApiResult exceptionResult;
                var ex = context.Exception;
                if (ex is DomainException domainException)
                {
                    exceptionResult = new ApiResult(domainException.ErrorCode, domainException.Message);
                    logger.LogWarning(ex, $"action failed due to domain exception");
                }
                else
                {
#if DEBUG
                    exceptionResult = new ApiResult(ErrorCode.UnknownError, $"Message:{ex.GetBaseException().Message}\r\nStackTrace:{ex.GetBaseException().StackTrace}");
#else
                    exceptionResult = new ApiResult(ErrorCode.UnknownError, ServerInternalError);
#endif
                    logger.LogError(ex, $"action failed due to exception");
                }
                context.Result = new JsonResult(exceptionResult);
                context.Exception = null;
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