using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace IFramework.JsonNetCore
{
    public static class JsonHelper
    {
        private static readonly ConcurrentDictionary<string, JsonSerializerSettings> SettingDictionary = new ConcurrentDictionary<string, JsonSerializerSettings>();

        private static readonly ILogger JsonLogger = IoCFactory.GetService<ILoggerFactory>().CreateLogger(typeof(JsonHelper).Name);

        internal static JsonSerializerSettings InternalGetCustomJsonSerializerSettings(bool serializeNonPulibc,
                                                                                       bool loopSerialize,
                                                                                       bool useCamelCase,
                                                                                       bool useStringEnumConvert,
                                                                                       bool ignoreSerializableAttribute,
                                                                                       bool ignoreNullValue,
                                                                                       bool lowerCase)
        {
            var customSettings = new JsonSerializerSettings
            {
                ContractResolver = new CustomContractResolver(serializeNonPulibc, lowerCase)
            };

            if (loopSerialize)
            {
                customSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                customSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            }
            else
            {
                customSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            }
            if (useStringEnumConvert)
            {
                customSettings.Converters.Add(new StringEnumConverter());
            }

            ((DefaultContractResolver) customSettings.ContractResolver).IgnoreSerializableAttribute = ignoreSerializableAttribute;

            if (useCamelCase)
            {
                if (customSettings.ContractResolver is DefaultContractResolver resolver)
                {
                    resolver.NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = true,
                        OverrideSpecifiedNames = true
                    };
                }
            }
            if (ignoreNullValue)
            {
                customSettings.NullValueHandling = NullValueHandling.Ignore;
            }
            return customSettings;
        }

        public static JsonSerializerSettings GetCustomJsonSerializerSettings(bool serializeNonPulibc,
                                                                             bool loopSerialize,
                                                                             bool useCamelCase,
                                                                             bool useStringEnumConvert = true,
                                                                             bool ignoreSerializableAttribute = true,
                                                                             bool ignoreNullValue = false,
                                                                             bool useCached = true,
                                                                             bool lowerCase = false)
        {

            JsonSerializerSettings settings = null;
            if (useCached)
            {
                var key = $"{serializeNonPulibc}{loopSerialize}{useCamelCase}{useStringEnumConvert}";
                settings = SettingDictionary.GetOrAdd(key,
                                                      k => InternalGetCustomJsonSerializerSettings(serializeNonPulibc,
                                                                                                   loopSerialize,
                                                                                                   useCamelCase,
                                                                                                   useStringEnumConvert,
                                                                                                   ignoreSerializableAttribute,
                                                                                                   ignoreNullValue,
                                                                                                   lowerCase));
            }
            else
            {
                settings = InternalGetCustomJsonSerializerSettings(serializeNonPulibc,
                                                                   loopSerialize,
                                                                   useCamelCase,
                                                                   useStringEnumConvert,
                                                                   ignoreSerializableAttribute,
                                                                   ignoreNullValue,
                                                                   lowerCase);
            }
            return settings;
        }

        public static string ToJson(this object obj,
                                    bool serializeNonPublic = false,
                                    bool loopSerialize = false,
                                    bool useCamelCase = false,
                                    bool ignoreNullValue = false,
                                    bool useStringEnumConvert = true)
        {
            return JsonConvert.SerializeObject(obj,
                                               GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase, useStringEnumConvert, ignoreNullValue: ignoreNullValue));
        }

        public static object ToJsonObject(this string json,
                                          bool serializeNonPublic = false,
                                          bool loopSerialize = false,
                                          bool useCamelCase = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }
                return JsonConvert.DeserializeObject(json,
                                                     GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase));
            }
            catch (Exception ex)
            {
                JsonLogger.LogError(ex, $"ToJsonObject Failed {json}");
                return null;
            }
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
                return JsonConvert.DeserializeObject(json,
                                                     jsonType,
                                                     GetCustomJsonSerializerSettings(serializeNonPublic,
                                                                                     loopSerialize,
                                                                                     useCamelCase));
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
                    return (T) json.ToDynamicObjects(serializeNonPublic,
                                                     loopSerialize,
                                                     useCamelCase);
                }
                if (typeof(T) == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic,
                                                loopSerialize,
                                                useCamelCase);
                }
                return JsonConvert.DeserializeObject<T>(json,
                                                        GetCustomJsonSerializerSettings(serializeNonPublic,
                                                                                        loopSerialize,
                                                                                        useCamelCase));
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
            return json.ToJsonObject<JObject>(serializeNonPublic, loopSerialize, useCamelCase);
        }

        public static IEnumerable<dynamic> ToDynamicObjects(this string json,
                                                            bool serializeNonPublic = false,
                                                            bool loopSerialize = false,
                                                            bool useCamelCase = false)
        {
            return json.ToJsonObject<JArray>(serializeNonPublic, loopSerialize);
        }
    }
}