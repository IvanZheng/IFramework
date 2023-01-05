using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace IFramework.Configuration.ProtectedJson
{
    public class ProtectedJsonConfigurationSource : JsonConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new ProtectedJsonConfigurationProvider(this);
        }
    }
}