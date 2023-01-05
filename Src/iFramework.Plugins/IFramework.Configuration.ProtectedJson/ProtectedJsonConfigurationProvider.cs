using Microsoft.Extensions.Configuration.Json;
using System;
using System.IO;
using System.Linq;

namespace IFramework.Configuration.ProtectedJson
{
    public class ProtectedJsonConfigurationProvider : JsonConfigurationProvider
    {
        private readonly string _secretKey;
        private readonly string _cipherPrefix;
        private readonly int __cipherPrefixSize;

        public ProtectedJsonConfigurationProvider(ProtectedJsonConfigurationSource source) : base(source)
        {
            _secretKey = Environment.GetEnvironmentVariable("PROTECTEDJSON_SECRET_KEY");
            _cipherPrefix = Environment.GetEnvironmentVariable("PROTECTEDJSON_CIPHER_PREFIX") ?? "cipherText:";
            __cipherPrefixSize = _cipherPrefix.Length;
        }

        public override void Load(Stream stream)
        {
            base.Load(stream);

            var result = Data.Where(kv => kv.Value.StartsWith(_cipherPrefix)).ToList();
            if (result != null && result.Count > 0)
            {
                foreach (var item in result)
                {
                    var value = Data[item.Key];
                    value = value[__cipherPrefixSize..];
                    try
                    {
                        Data[item.Key] = AesEncryptor.Decrypt(value, _secretKey);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Appsettings decrypt failed. key = {item.Key}.", e);
                    }
                }
            }
        }
    }
}
