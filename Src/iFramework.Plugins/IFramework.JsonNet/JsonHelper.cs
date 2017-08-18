using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IFramework.Infrastructure.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using IFramework.IoC;

namespace IFramework.JsonNet
{
    public class NonPublicContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            //TODO: Maybe cache
            var prop = base.CreateProperty(member, memberSerialization);

            if (!prop.Writable)
            {
                var property = member as PropertyInfo;
                if (property != null)
                {
                    var hasPrivateSetter = property.GetSetMethod(true) != null;
                    prop.Writable = hasPrivateSetter;
                }
            }
            return prop;
        }
    }

    public static class JsonHelper
    {
        private static readonly ConcurrentDictionary<string, JsonSerializerSettings> SettingDictionary = new ConcurrentDictionary<string, JsonSerializerSettings>();

        private static readonly ILogger _JsonLogger = IoCFactory.IsInit()
                                                          ? IoCFactory.Resolve<ILoggerFactory>().Create(typeof(JsonHelper).Name)
                                                          : null;

        internal static JsonSerializerSettings InternalGetCustomJsonSerializerSettings(bool serializeNonPulibc,
                                                                                       bool loopSerialize,
                                                                                       bool useCamelCase,
                                                                                       bool useStringEnumConvert,
                                                                                       bool ignoreSerializableAttribute,
                                                                                       bool ignoreNullValue)
        {
            var customSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver()
            };
            if (serializeNonPulibc)
            {
                customSettings.ContractResolver = new NonPublicContractResolver();
            }
            if (loopSerialize)
            {
                customSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                customSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            }
            if (useStringEnumConvert)
            {
                customSettings.Converters.Add(new StringEnumConverter());
            }
          
            ((DefaultContractResolver)customSettings.ContractResolver).IgnoreSerializableAttribute = ignoreSerializableAttribute;
            
            if (useCamelCase)
            {
                var resolver = customSettings.ContractResolver as DefaultContractResolver;
                if (resolver != null)
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
                                                                             bool useCached = true)
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
                                                                                                   ignoreNullValue));
            }
            else
            {
                settings = InternalGetCustomJsonSerializerSettings(serializeNonPulibc,
                                                                   loopSerialize,
                                                                   useCamelCase,
                                                                   useStringEnumConvert,
                                                                   ignoreSerializableAttribute,
                                                                   ignoreNullValue);
            }
            return settings;
        }

        public static string ToJson(this object obj,
                                    bool serializeNonPublic = true,
                                    bool loopSerialize = false,
                                    bool useCamelCase = false,
                                    bool ignoreNullValue = false)
        {
            return JsonConvert.SerializeObject(obj,
                                               GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase, ignoreNullValue: ignoreNullValue));
        }

        public static object ToJsonObject(this string json,
                                          bool serializeNonPublic = true,
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
                _JsonLogger?.Error($"ToJsonObject Failed {json}", ex);
                return null;
            }
        }

        public static object ToJsonObject(this string json,
                                          Type jsonType,
                                          bool serializeNonPublic = true,
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
                return JsonConvert.DeserializeObject(json, jsonType,
                                                     GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase));
            }
            catch (Exception ex)
            {
                _JsonLogger?.Error($"ToJsonObject Failed {json}", ex);
                return null;
            }
        }

        public static T ToJsonObject<T>(this string json,
                                        bool serializeNonPublic = true,
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
                    return (T) (object) json.ToDynamicObjects(serializeNonPublic, loopSerialize, useCamelCase);
                }
                if (typeof(T) == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize, useCamelCase);
                }
                return JsonConvert.DeserializeObject<T>(json,
                                                        GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase));
            }
            catch (Exception ex)
            {
                _JsonLogger?.Error($"ToJsonObject Failed {json}", ex);
                return default(T);
            }
        }

        public static dynamic ToDynamicObject(this string json,
                                              bool serializeNonPublic = true,
                                              bool loopSerialize = false,
                                              bool useCamelCase = false)
        {
            return json.ToJsonObject<JObject>(serializeNonPublic, loopSerialize, useCamelCase);
        }

        public static List<dynamic> ToDynamicObjects(this string json,
                                                     bool serializeNonPublic = true,
                                                     bool loopSerialize = false,
                                                     bool useCamelCase = false)
        {
            return json.ToJsonObject<JArray>(serializeNonPublic, loopSerialize)
                       .Cast<dynamic>()
                       .ToList();
        }
    }
}