using System.Threading.Tasks;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sample.CommandServiceCore.AuthorizationHandlers
{
    public class AppAuthorizationHandler : AuthorizationHandler<AppAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AppAuthorizationRequirement requirement)
        {
            if (!(context.Resource is AuthorizationFilterContext filterContext))
            {
                return Task.CompletedTask;
            }
            //context.Succeed(requirement);
            //if (httpContext?.User.Identity.IsAuthenticated ?? false)
            //{
            //    context.Succeed(requirement); 
            //}
            //else
            //{
            //    context.Fail();
            //}
            filterContext.Result = new ObjectResult(new ApiResult(500, "Authorization Handler Handle failed!"));
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}