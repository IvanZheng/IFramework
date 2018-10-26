using System.Net;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sample.CommandServiceCore.Authorizations
{
    public class AppAuthorizationHandler : AuthorizationHandler<AppAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AppAuthorizationRequirement requirement)
        {
            if (!(context.Resource is AuthorizationFilterContext filterContext))
            {
                return Task.CompletedTask;
            }

            if (string.IsNullOrEmpty(filterContext.HttpContext.Request.Query["token"]))
            {
                filterContext.Result = new JsonResult(new ApiResult((int)HttpStatusCode.Forbidden, "Authorization Handler Handle failed!"));
                filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}