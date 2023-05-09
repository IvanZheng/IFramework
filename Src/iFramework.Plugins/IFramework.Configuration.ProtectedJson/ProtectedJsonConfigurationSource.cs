using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace IFramework.Configuration.ProtectedJson
{
    public class ProtectedJsonConfigurationSource : JsonConfigurationSource
    {
        /// <summary>
        /// 用于解密的密钥
        /// </summary>
        public string SecretKeyName { get; set; } = default!;

        /// <summary>
        /// 密文前缀
        /// </summary>
        public string CipherPrefix { get; set; } = default!;


        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new ProtectedJsonConfigurationProvider(this);
        }
    }
}