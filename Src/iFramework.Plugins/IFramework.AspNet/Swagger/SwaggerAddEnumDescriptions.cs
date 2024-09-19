using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IFramework.AspNet.Swagger
{
    public class SwaggerAddEnumDescriptions : IDocumentFilter
    {
        private readonly string _namespace;

        public SwaggerAddEnumDescriptions(string @namespace)
        {
            _namespace = @namespace;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // add enum descriptions to result models
            foreach (var property in swaggerDoc.Components.Schemas.Where(x => x.Value?.Enum?.Count > 0))
            {
                IList<IOpenApiAny> propertyEnums = property.Value.Enum;
                if (propertyEnums is { Count: > 0 })
                {
                    property.Value.Description += Environment.NewLine + DescribeEnum(propertyEnums, property.Key);
                }
            }

            // add enum descriptions to input parameters
            foreach (var pathItem in swaggerDoc.Paths.Values)
            {
                DescribeEnumParameters(pathItem.Operations, swaggerDoc);
            }
        }

        private void DescribeEnumParameters(IDictionary<OperationType, OpenApiOperation> operations, OpenApiDocument swaggerDoc)
        {
            if (operations != null)
            {
                foreach (var operation in operations)
                {
                    foreach (var param in operation.Value.Parameters)
                    {
                        var paramEnum = swaggerDoc.Components.Schemas.FirstOrDefault(x => x.Key == param.Schema.Reference?.Id);
                        if (paramEnum.Value != null)
                        {
                            param.Description += Environment.NewLine + DescribeEnum(paramEnum.Value.Enum, paramEnum.Key);
                        }
                    }
                }
            }
        }

        private Type GetEnumTypeByName(string enumTypeName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(x => x.FullName?.StartsWith(_namespace) ?? false)
                .SelectMany(x => x.GetTypes().Where(t => t.IsEnum))
                .FirstOrDefault(x => x.Name == enumTypeName);
        }

        private string DescribeEnum(IList<IOpenApiAny> enums, string propertyTypeName)
        {
            List<string> enumDescriptions = new List<string>();
            var enumType = GetEnumTypeByName(propertyTypeName);
            if (enumType == null)
                return null;

            foreach (var openApiAny in enums)
            {
                if (openApiAny is OpenApiInteger enumOptionInteger)
                {
                    var enumValue = Enum.Parse(enumType, Enum.GetName(enumType, enumOptionInteger.Value) ?? string.Empty);

                    enumDescriptions.Add($"{enumValue} = {(int)enumValue} {((Enum)enumValue).GetDescriptionAttribute()?.Description}");
                }
                else if (openApiAny is OpenApiString enumOptionString)
                {
                    var enumValue = Enum.Parse(enumType, enumOptionString.Value);
                    enumDescriptions.Add($"{enumValue} = {(int)enumValue} {((Enum)enumValue).GetDescriptionAttribute()?.Description}");
                }
            }

            return string.Join($",{Environment.NewLine}", enumDescriptions.ToArray());
        }
    }
}
