using System;
using System.Collections.Generic;
using System.ComponentModel;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Infrastructure
{
    public static class JsonHelper
    {
        private static IJsonConvert JsonConvert => ObjectProviderFactory.GetService<IJsonConvert>();
        private static ILogger JsonLogger => ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(typeof(JsonHelper));

        public static string ToJson(this object obj,
                                    bool serializeNonPublic = false,
                                    bool loopSerialize = false,
                                    bool useCamelCase = false,
                                    bool ignoreNullValue = true,
                                    bool processDictionaryKeys = true)
        {
            return JsonConvert.SerializeObject(obj, serializeNonPublic, loopSerialize, useCamelCase, ignoreNullValue, processDictionaryKeys:processDictionaryKeys);
        }

        public static object ToJsonObject(this string json,
                                          bool serializeNonPublic = false,
                                          bool loopSerialize = false,
                                          bool useCamelCase = false,
                                          bool processDictionaryKeys = true)
        {
            return JsonConvert.DeserializeObject(json, serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys:processDictionaryKeys);
        }

        public static object ToJsonObject(this string json,
                                          Type jsonType,
                                          bool serializeNonPublic = false,
                                          bool loopSerialize = false,
                                          bool useCamelCase = false,
                                          bool processDictionaryKeys = true)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            try
            {
                json = json.Trim();
                if (jsonType == typeof(string) && (!json.StartsWith("\"") && !json.StartsWith("'") || !json.EndsWith("\"") && !json.EndsWith("'")))
                {
                    return json;
                }
                if (jsonType == typeof(List<dynamic>))
                {
                    return json.ToDynamicObjects(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
                }
                if (jsonType == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
                }

                if (jsonType.IsPrimitive || jsonType == typeof(Guid))
                {
                    if (!json.StartsWith("\"") && !json.StartsWith("'") || !json.EndsWith("\"") && !json.EndsWith("'"))
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(jsonType);
                        if (converter.CanConvertFrom(typeof(string)))
                        {
                            return converter.ConvertFromInvariantString(json);
                        }
                    }
                }
                return JsonConvert.DeserializeObject(json, jsonType, serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys:processDictionaryKeys);
            }
            catch (Exception ex)
            {
                JsonLogger.LogError(ex, $"ToJsonObject Failed {json}");
                return null;
            }
        }

        public static T ToJsonObject<T>(this string json,
                                        bool serializeNonPublic = false,
                                        bool loopSerialize = false,
                                        bool useCamelCase = false,
                                        bool processDictionaryKeys = true)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default(T);
            }
            try
            {
                var jsonType = typeof(T);
                json = json.Trim();
                if (jsonType == typeof(string) && (!json.StartsWith("\"") && !json.StartsWith("'") || !json.EndsWith("\"") && !json.EndsWith("'")))
                {
                    return (T)(object)json;
                }
                if (typeof(T) == typeof(IEnumerable<dynamic>))
                {
                    return (T)json.ToDynamicObjects(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
                }
                if (typeof(T) == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
                }
                if (jsonType.IsPrimitive || jsonType == typeof(Guid))
                {
                    if (!json.StartsWith("\"") && !json.StartsWith("'") || !json.EndsWith("\"") && !json.EndsWith("'"))
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(jsonType);
                        if (converter.CanConvertFrom(typeof(string)))
                        {
                            return (T)converter.ConvertFromInvariantString(json);
                        }
                    }
                }
                return JsonConvert.DeserializeObject<T>(json, serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
            }
            catch (Exception ex)
            {
                JsonLogger.LogError(ex, $"ToJsonObject Failed {json}");
                return default(T);
            }
        }

        public static dynamic ToDynamicObject(this string json,
                                              bool serializeNonPublic = false,
                                              bool loopSerialize = false,
                                              bool useCamelCase = false,
                                              bool processDictionaryKeys = true)
        {
            return JsonConvert.DeserializeDynamicObject(json, serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
        }

        public static IEnumerable<dynamic> ToDynamicObjects(this string json,
                                                     bool serializeNonPublic = false,
                                                     bool loopSerialize = false,
                                                     bool useCamelCase = false, 
                                                     bool processDictionaryKeys = true)
        {
            return JsonConvert.DeserializeDynamicObjects(json, serializeNonPublic, loopSerialize, useCamelCase, processDictionaryKeys);
        }
    }
}