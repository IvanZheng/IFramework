using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IFramework.AspNet.Swagger
{
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        private readonly string _apiName;

        public AuthorizeCheckOperationFilter(string apiName)
        {
            _apiName = apiName;
        }
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo?.DeclaringType?.GetCustomAttributes(true)
                                      .Union(context.MethodInfo.GetCustomAttributes(true))
                                      .OfType<AuthorizeAttribute>().Any();

            if (hasAuthorize ?? false)
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
                var oAuthScheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "oauth2"
                    }
                };
                operation.Security = new List<OpenApiSecurityRequirement> {
                    new OpenApiSecurityRequirement {
                        [oAuthScheme] = new[] { _apiName }
                    }
                };
            }
        }
    }
}
