using System;
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
        private static readonly ILogger _JsonLogger = IoCFactory.IsInit()
                                                          ? IoCFactory.Resolve<ILoggerFactory>().Create(typeof(JsonHelper).Name)
                                                          : null;

        private static JsonSerializerSettings _NonPublicSerializerSettings;

        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings();

        private static readonly JsonSerializerSettings LoopSerializeSerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

        private static JsonSerializerSettings _NonPublicLoopSerializeSerializerSettings;

        private static JsonSerializerSettings NonPublicSerializerSettings
        {
            get
            {
                if (_NonPublicSerializerSettings == null)
                {
                    _NonPublicSerializerSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new NonPublicContractResolver()
                    };
                }
                return _NonPublicSerializerSettings;
            }
        }

        private static JsonSerializerSettings NonPublicLoopSerializeSerializerSettings
        {
            get
            {
                if (_NonPublicLoopSerializeSerializerSettings == null)
                {
                    _NonPublicLoopSerializeSerializerSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new NonPublicContractResolver(),
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                    };
                }
                return _NonPublicLoopSerializeSerializerSettings;
            }
        }

        public static JsonSerializerSettings GetCustomJsonSerializerSettings(bool serializeNonPulibc,
                                                                             bool loopSerialize,
                                                                             bool useCamelCase)
        {
            JsonSerializerSettings customSettings = null;
            if (serializeNonPulibc && loopSerialize)
            {
                customSettings = NonPublicLoopSerializeSerializerSettings;
            }
            else if (serializeNonPulibc)
            {
                customSettings = NonPublicSerializerSettings;
            }
            else if (loopSerialize)
            {
                customSettings = LoopSerializeSerializerSettings;
            }
            else
            {
                customSettings = DefaultSerializerSettings;
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

        public static string ToJson(this object obj,
                                    bool serializeNonPublic = false,
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