using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace IFramework.Infrastructure
{
    internal class DataContractJsonConvert : IJsonConvert
    {
        public string SerializeObject(object value, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool ignoreNullValue = true, bool useStringEnumConvert = true)
        {
            var json = new DataContractJsonSerializer(value.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                json.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public object DeserializeObject(string value, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            throw new NotImplementedException();
        }

        public dynamic DeserializeDynamicObject(string json, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            throw new NotImplementedException();
        }

        public dynamic DeserializeDynamicObjects(string json, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            throw new NotImplementedException();
        }

        public T DeserializeAnonymousType<T>(string value, T anonymousTypeObject, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            throw new NotImplementedException();
        }

        public T DeserializeObject<T>(string value, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            return (T) DeserializeObject(value, typeof(T), serializeNonPublic, loopSerialize, useCamelCase);
        }

        public object DeserializeObject(string value, Type type, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            {
                var serializer = new DataContractJsonSerializer(type);
                return serializer.ReadObject(ms);
            }
        }

        public void PopulateObject(string value, object target, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            throw new NotImplementedException();
        }

        public string SerializeXmlNode(XmlNode node)
        {
            throw new NotImplementedException();
        }

        public XmlDocument DeserializeXmlNode(string value)
        {
            throw new NotImplementedException();
        }

        public XmlDocument DeserializeXmlNode(string value, string deserializeRootElementName)
        {
            throw new NotImplementedException();
        }

        public XmlDocument DeserializeXmlNode(string value, string deserializeRootElementName, bool writeArrayAttribute)
        {
            throw new NotImplementedException();
        }

        public string SerializeXNode(XObject node)
        {
            throw new NotImplementedException();
        }

        public XDocument DeserializeXNode(string value)
        {
            throw new NotImplementedException();
        }

        public XDocument DeserializeXNode(string value, string deserializeRootElementName)
        {
            throw new NotImplementedException();
        }

        public XDocument DeserializeXNode(string value, string deserializeRootElementName, bool writeArrayAttribute)
        {
            throw new NotImplementedException();
        }
    }

    public class MicrosoftJsonConvert : IJsonConvert
    {
        public string SerializeObject(object value, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool ignoreNullValue = true, bool useStringEnumConvert = true)
        {
            return JsonSerializer.ToString(value, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = false,
                IgnoreReadOnlyProperties = false,
                IgnoreNullValues = ignoreNullValue,
                PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null
            });
        }

        public object DeserializeObject(string value, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            return JsonSerializer.Parse(value, typeof(object), new JsonSerializerOptions {
                PropertyNameCaseInsensitive = false,
                IgnoreReadOnlyProperties = false,
                PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null
            });
        }

        public dynamic DeserializeDynamicObject(string json, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            return JsonSerializer.Parse(json, typeof(object), new JsonSerializerOptions {
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null,
                IgnoreReadOnlyProperties = false
            });
        }

        public dynamic DeserializeDynamicObjects(string json, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            return DeserializeDynamicObject(json, serializeNonPublic, loopSerialize, useCamelCase);
        }

        public T DeserializeAnonymousType<T>(string value, T anonymousTypeObject, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            return JsonSerializer.Parse<T>(value, new JsonSerializerOptions {
                IgnoreReadOnlyProperties = false,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null
            });
        }

        public T DeserializeObject<T>(string value, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            return JsonSerializer.Parse<T>(value, new JsonSerializerOptions {
                IgnoreReadOnlyProperties = false,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null
            });
        }

        public object DeserializeObject(string value, Type type, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            return JsonSerializer.Parse(value, type, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = false,
                IgnoreReadOnlyProperties = false,
                PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null
            });
        }

        public void PopulateObject(string value, object target, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            throw new NotImplementedException();
        }

        public string SerializeXmlNode(XmlNode node)
        {
            throw new NotImplementedException();
        }

        public XmlDocument DeserializeXmlNode(string value)
        {
            throw new NotImplementedException();
        }

        public XmlDocument DeserializeXmlNode(string value, string deserializeRootElementName)
        {
            throw new NotImplementedException();
        }

        public XmlDocument DeserializeXmlNode(string value, string deserializeRootElementName, bool writeArrayAttribute)
        {
            throw new NotImplementedException();
        }

        public string SerializeXNode(XObject node)
        {
            throw new NotImplementedException();
        }

        public XDocument DeserializeXNode(string value)
        {
            throw new NotImplementedException();
        }

        public XDocument DeserializeXNode(string value, string deserializeRootElementName)
        {
            throw new NotImplementedException();
        }

        public XDocument DeserializeXNode(string value, string deserializeRootElementName, bool writeArrayAttribute)
        {
            throw new NotImplementedException();
        }
    }
}