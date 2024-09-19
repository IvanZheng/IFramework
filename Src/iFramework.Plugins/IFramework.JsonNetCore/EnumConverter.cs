using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace IFramework.JsonNet
{
    public class EnumConverter : JsonConverter
    {
        private readonly Type[] _excludeTypes;

        public EnumConverter(params Type[] excludeTypes)
        {
            _excludeTypes = excludeTypes;
        }

        public string EnumValuePropertyName { get; set; } = "value";
        public string EnumDescriptionPropertyName { get; set; } = "description";
        public bool EnumValueToString { get; set; } = true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null)
            {
                var enumType = value.GetType();
                var enumName = Enum.GetName(enumType, value);
                if (string.IsNullOrWhiteSpace(enumName))
                {
                    return;
                }
                object enumValue = EnumValueToString ? enumName : (int)value;
                var description = enumType.GetField(enumName ?? string.Empty)
                                          ?
                                          .GetCustomAttribute<DescriptionAttribute>()
                                          ?.Description ?? enumValue?.ToString();
                var enumObject = new Dictionary<string, object>
                {
                    [EnumValuePropertyName] = enumValue, 
                    [EnumDescriptionPropertyName] = description
                };
                serializer.Serialize(writer, enumObject);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                if (reader is { TokenType: JsonToken.String, Value: not null })
                {
                    return Enum.Parse(objectType, reader.Value?.ToString() ?? string.Empty);
                }

                if (reader is { TokenType: JsonToken.Integer, Value: not null })
                {
                    return Enum.ToObject(objectType, reader.ValueType == typeof(long) ? (long)reader.Value : (int)reader.Value);
                }

                return null;
            }
            catch (Exception e)
            {
                throw new InputFormatterException(e.Message, e);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsNullableType())
            {
                objectType = Nullable.GetUnderlyingType(objectType);
            }

            return objectType is { IsEnum: true } && !_excludeTypes.Contains(objectType);
        }
    }
}