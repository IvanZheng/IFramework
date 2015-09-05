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
            this.DefaultMembersSearchFlags |= BindingFlags.NonPublic;
        }

        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            return base.GetSerializableMembers(objectType);
        }
    }
    public static class JsonHelper
    {
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

        public static JsonSerializerSettings GetCustomJsonSerializerSettings(bool serializeNonPulibc, bool loopSerialize)
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

            return customSettings;
        }

        public static string ToJson(this object obj, bool serializeNonPublic = false, bool loopSerialize = false)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize));
        }

        public static object ToJsonObject(this string json, bool serializeNonPublic = true, bool loopSerialize = false)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject(json, GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize));
            }
            catch (System.Exception)
            {
                return null;
            }

        }

        public static object ToJsonObject(this string json, Type jsonType, bool serializeNonPublic = true, bool loopSerialize = false)
        {
            try
            {
                if (jsonType == typeof(List<dynamic>))
                {
                    return (object)(json.ToDynamicObjects(serializeNonPublic, loopSerialize));
                }
                else if (jsonType == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize);
                }
                else
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject(json, jsonType, GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize));
                }
            }
            catch (System.Exception)
            {
                return null;
            }

        }

        public static T ToJsonObject<T>(this string json, bool serializeNonPublic = true, bool loopSerialize = false)
        {
            try
            {
                if (typeof(T) == typeof(List<dynamic>))
                {
                    return (T)(object)(json.ToDynamicObjects(serializeNonPublic, loopSerialize));
                }
                else if (typeof(T) == typeof(object))
                {
                    return json.ToDynamicObject(serializeNonPublic, loopSerialize);
                }
                else
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, GetCustomJsonSerializerSettings(serializeNonPublic, loopSerialize));
                }
            }
            catch (System.Exception)
            {
                return default(T);
            }

        }

        public static dynamic ToDynamicObject(this string json, bool serializeNonPublic = true, bool loopSerialize = false)
        {
            dynamic dj = null;
            var jsonObj = json.ToJsonObject<JObject>(serializeNonPublic, loopSerialize);
            if (jsonObj != null)
            {
                dj = new DynamicJson(jsonObj);
            }
            return dj;
        }

        public static List<dynamic> ToDynamicObjects(this string json, bool serializeNonPublic = true, bool loopSerialize = false)
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
