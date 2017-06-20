using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace IFramework.Infrastructure
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
                                                                                       bool useCamelCase)
        {
            var customSettings = new JsonSerializerSettings();
            if (serializeNonPulibc)
            {
                customSettings.ContractResolver = new NonPublicContractResolver();
            }
            if (loopSerialize)
            {
                customSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                customSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            }
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
            return customSettings;
        }

        public static JsonSerializerSettings GetCustomJsonSerializerSettings(bool serializeNonPulibc,
                                                                             bool loopSerialize,
                                                                             bool useCamelCase,
                                                                             bool useCached = true)
        {
            JsonSerializerSettings settings = null;
            if (useCached)
            {
                var key = $"{serializeNonPulibc}{loopSerialize}{useCamelCase}";
                settings = SettingDictionary.GetOrAdd(key,
                                                      k => InternalGetCustomJsonSerializerSettings(serializeNonPulibc,
                                                                                                   loopSerialize,
                                                                                                   useCamelCase));
            }
            else
            {
                settings = InternalGetCustomJsonSerializerSettings(serializeNonPulibc, loopSerialize, useCamelCase);
            }
            return settings;
        }

        public static string ToJson(this object obj,
                                    bool serializeNonPublic = true,
                                    bool loopSerialize = false,
                                    bool useCamelCase = false)
        {
            return JsonConvert.SerializeObject(obj,
                                               GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase));
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
            dynamic dj = null;
            var jsonObj = json.ToJsonObject<JObject>(serializeNonPublic, loopSerialize);
            if (jsonObj != null)
            {
                dj = new DynamicJson(jsonObj);
            }
            return dj;
        }

        public static List<dynamic> ToDynamicObjects(this string json,
                                                     bool serializeNonPublic = true,
                                                     bool loopSerialize = false,
                                                     bool useCamelCase = false)
        {
            List<dynamic> djs = null;
            var jsonObj = json.ToJsonObject<JArray>(serializeNonPublic, loopSerialize);
            if (jsonObj != null)
            {
                djs = new List<dynamic>();
                foreach (var j in jsonObj)
                {
                    djs.Add(new DynamicJson(j as JObject));
                }
            }
            return djs;
        }
    }
}