using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sample.CommandServiceCore.Authorizations
{
    public class AuthorizationFilterAttribute : ActionFilterAttribute
    {
        public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            context.Result = new ObjectResult(new ApiResult((int)HttpStatusCode.Forbidden, "Authorization Handler Handle failed!"));
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return Task.CompletedTask;
            //return base.OnActionExecutionAsync(context, next);
        }
    }
}
