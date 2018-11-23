using System;
using System.Collections.Generic;
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
                                    bool ignoreNullValue = true)
        {
            return JsonConvert.SerializeObject(obj, serializeNonPublic, loopSerialize, useCamelCase, ignoreNullValue);
        }

        public static object ToJsonObject(this string json,
                                          bool serializeNonPublic = false,
                                          bool loopSerialize = false,
                                          bool useCamelCase = false)
        {
            return JsonConvert.DeserializeObject(json, serializeNonPublic, loopSerialize, useCamelCase);
        }

        public static object ToJsonObject(this string json,
                                          Type jsonType,
                                          bool serializeNonPublic = false,
                                          bool loopSerialize = false,
                                          bool useCamelCase = false)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            try
            {
                if (jsonType == typeof(List<dynamic>))
                {
                    return json.ToDynamicObjects(serializeNonPublic, loopSerialize, useCamelCase);
                }
                if (jsonType == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize, useCamelCase);
                }
                return JsonConvert.DeserializeObject(json, jsonType, serializeNonPublic, loopSerialize, useCamelCase);
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
                                        bool useCamelCase = false)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default(T);
            }
            try
            {
                if (typeof(T) == typeof(List<dynamic>))
                {
                    return (T)(object)json.ToDynamicObjects(serializeNonPublic, loopSerialize, useCamelCase);
                }
                if (typeof(T) == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize, useCamelCase);
                }
                return JsonConvert.DeserializeObject<T>(json, serializeNonPublic, loopSerialize, useCamelCase);
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
                                              bool useCamelCase = false)
        {
            return JsonConvert.DeserializeDynamicObject(json, serializeNonPublic, loopSerialize, useCamelCase);
        }

        public static List<dynamic> ToDynamicObjects(this string json,
                                                     bool serializeNonPublic = false,
                                                     bool loopSerialize = false,
                                                     bool useCamelCase = false)
        {
            return JsonConvert.DeserializeDynamicObjects(json, serializeNonPublic, loopSerialize, useCamelCase);
        }
    }
}