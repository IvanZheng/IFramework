using System;
using System.Xml;
using System.Xml.Linq;
using IFramework.Infrastructure;
using Newtonsoft.Json;

namespace IFramework.JsonNet
{
    public class JsonConvertImpl: IJsonConvert
    {
        public string SerializeObject(object value, bool serializeNonPublic = false, 
                                      bool loopSerialize = false, bool useCamelCase = false,
                                      bool ignoreNullValue = true, bool useStringEnumConvert = true,
                                      bool processDictionaryKeys = true)
        {
            return value.ToJson(serializeNonPublic, loopSerialize, useCamelCase, ignoreNullValue, useStringEnumConvert, processDictionaryKeys);
        }

        public object DeserializeObject(string value, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool processDictionaryKeys = true)
        {
            return value.ToObject(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
        }

        public dynamic DeserializeDynamicObject(string json, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool processDictionaryKeys = true)
        {
            return json.ToDynamicObject(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
        }

        public dynamic DeserializeDynamicObjects(string json, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool processDictionaryKeys = true)
        {
            return json.ToDynamicObjects(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
        }

        public T DeserializeAnonymousType<T>(string value, T anonymousTypeObject, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool processDictionaryKeys = true)
        {
            return JsonConvert.DeserializeAnonymousType(value, anonymousTypeObject, 
                                                        JsonHelper.GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys:processDictionaryKeys));

        }

        public T DeserializeObject<T>(string value, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool processDictionaryKeys = true)
        {
            return value.ToObject<T>(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
        }

        public object DeserializeObject(string value, Type type, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool processDictionaryKeys = true)
        {
            return value.ToObject(type, serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
        }

        public void PopulateObject(string value, object target, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false, bool processDictionaryKeys = true)
        {
            JsonConvert.PopulateObject(value, target, JsonHelper.GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys:processDictionaryKeys));
        }

        public string SerializeXmlNode(XmlNode node)
        {
            return JsonConvert.SerializeXmlNode(node);
        }

        public XmlDocument DeserializeXmlNode(string value)
        {
            return JsonConvert.DeserializeXmlNode(value);
        }

        public XmlDocument DeserializeXmlNode(string value, string deserializeRootElementName)
        {
            return JsonConvert.DeserializeXmlNode(value, deserializeRootElementName);
        }

        public XmlDocument DeserializeXmlNode(string value, string deserializeRootElementName, bool writeArrayAttribute)
        {
            return JsonConvert.DeserializeXmlNode(value, deserializeRootElementName, writeArrayAttribute);
        }

        public string SerializeXNode(XObject node)
        {
            return JsonConvert.SerializeXNode(node);
        }
        
        public XDocument DeserializeXNode(string value)
        {
            return JsonConvert.DeserializeXNode(value);
        }

        public XDocument DeserializeXNode(string value, string deserializeRootElementName)
        {
            return JsonConvert.DeserializeXNode(value, deserializeRootElementName);
        }

        public XDocument DeserializeXNode(string value, string deserializeRootElementName, bool writeArrayAttribute)
        {
            return JsonConvert.DeserializeXNode(value, deserializeRootElementName, writeArrayAttribute);
        }
    }
}
