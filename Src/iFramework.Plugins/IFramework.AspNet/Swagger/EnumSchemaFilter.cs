using System;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IFramework.AspNet.Swagger
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                schema.Enum.Clear();
                var enumNames = Enum.GetNames(context.Type);
                schema.Type = "string";
                schema.Enum = enumNames.Select(v => new OpenApiString(v)).Cast<IOpenApiAny>().ToList();
            }
        }
    }

}
