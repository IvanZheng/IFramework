using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IFramework.Infrastructure
{
    public class NonPublicContractResolver : DefaultContractResolver
    {
        public NonPublicContractResolver()
        {
            //this.DefaultMembersSearchFlags |= BindingFlags.NonPublic;
        }

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
        static ILogger _JsonLogger = IoCFactory.IsInit() ? IoCFactory.Resolve<ILoggerFactory>().Create(typeof(JsonHelper).Name) : null;
        static JsonSerializerSettings _NonPublicSerializerSettings;
        static JsonSerializerSettings NonPublicSerializerSettings
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

        static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings();

        static readonly JsonSerializerSettings LoopSerializeSerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize
        };

        static JsonSerializerSettings _NonPublicLoopSerializeSerializerSettings;
        static JsonSerializerSettings NonPublicLoopSerializeSerializerSettings
        {
            get
            {
                if (_NonPublicLoopSerializeSerializerSettings == null)
                {
                    _NonPublicLoopSerializeSerializerSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new NonPublicContractResolver(),
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize
                    };
                }
                return _NonPublicLoopSerializeSerializerSettings;
            }
        }

        public static JsonSerializerSettings GetCustomJsonSerializerSettings(bool serializeNonPulibc, bool loopSerialize, bool useCamelCase)
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

        public static string ToJson(this object obj, bool serializeNonPublic = false, bool loopSerialize = false, bool useCamelCase = false)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase));
        }

        public static object ToJsonObject(this string json, bool serializeNonPublic = true, bool loopSerialize = false, bool useCamelCase = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }
                return Newtonsoft.Json.JsonConvert.DeserializeObject(json, GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase));
            }
            catch (System.Exception ex)
            {
                _JsonLogger?.Error($"ToJsonObject Failed {json}", ex);
                return null;
            }

        }

        public static object ToJsonObject(this string json, Type jsonType, bool serializeNonPublic = true, bool loopSerialize = false, bool useCamelCase = false)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            try
            {
                if (jsonType == typeof(List<dynamic>))
                {
                    return (object)(json.ToDynamicObjects(serializeNonPublic, loopSerialize, useCamelCase));
                }
                else if (jsonType == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize, useCamelCase);
                }
                else
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject(json, jsonType, GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase));
                }
            }
            catch (System.Exception ex)
            {
                _JsonLogger?.Error($"ToJsonObject Failed {json}", ex);
                return null;
            }

        }

        public static T ToJsonObject<T>(this string json, bool serializeNonPublic = true, bool loopSerialize = false, bool useCamelCase = false)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default(T);
            }
            try
            {
                if (typeof(T) == typeof(List<dynamic>))
                {
                    return (T)(object)(json.ToDynamicObjects(serializeNonPublic, loopSerialize, useCamelCase));
                }
                else if (typeof(T) == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize, useCamelCase);
                }
                else
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize, useCamelCase));
                }
            }
            catch (System.Exception ex)
            {
                _JsonLogger?.Error($"ToJsonObject Failed {json}", ex);
                return default(T);
            }

        }

        public static dynamic ToDynamicObject(this string json, bool serializeNonPublic = true, bool loopSerialize = false, bool useCamelCase = false)
        {
            dynamic dj = null;
            var jsonObj = json.ToJsonObject<JObject>(serializeNonPublic, loopSerialize);
            if (jsonObj != null)
            {
                dj = new DynamicJson(jsonObj);
            }
            return dj;
        }

        public static List<dynamic> ToDynamicObjects(this string json, bool serializeNonPublic = true, bool loopSerialize = false, bool useCamelCase = false)
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
