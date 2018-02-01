using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IFramework.JsonNetCore
{
    public class CustomContractResolver : DefaultContractResolver
    {
        private readonly bool _lowerCase;
        private readonly bool _serializeNonPulibc;

        public CustomContractResolver(bool serializeNonPulibc, bool lowerCase)
        {
            _serializeNonPulibc = serializeNonPulibc;
            _lowerCase = lowerCase;
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            return _lowerCase ? propertyName.ToLower() : propertyName;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            if (_serializeNonPulibc)
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
            return base.CreateProperty(member, memberSerialization);
        }
    }
}