using System;
using System.Collections.Generic;
using System.Reflection;
using IFramework.Event;
using IFramework.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace IFramework.JsonNetCore
{
    public class DomainExceptionConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            DomainException domainException;
            domainException = new DomainException((int)jo[nameof(domainException.ErrorCode)],
                                                  (string)jo[nameof(domainException.Message)]);
            return domainException;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DomainException);
        }
    }

    public class CustomContractResolver : DefaultContractResolver
    {
        private readonly bool _lowerCase;
        private readonly string[] _ignoreProperties;
        private readonly bool _serializeNonPulibc;

        public CustomContractResolver(bool serializeNonPulibc, bool lowerCase, params string[] ignoreProperties)
        {
            _serializeNonPulibc = serializeNonPulibc;
            _lowerCase = lowerCase;
            _ignoreProperties = ignoreProperties;
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            return _lowerCase ? propertyName.ToLower() : propertyName;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            if (_serializeNonPulibc)
            {
                //TODO: Maybe cache
               
                if (!prop.Writable)
                {
                    var property = member as PropertyInfo;
                    if (property != null)
                    {
                        var hasPrivateSetter = property.GetSetMethod(true) != null;
                        prop.Writable = hasPrivateSetter;
                    }
                }
            }

            if (typeof(Exception).IsAssignableFrom(prop.DeclaringType) && prop.PropertyName == "TargetSite")
            {
                prop.Ignored = true;
            }
            return prop;
        }
    }
}