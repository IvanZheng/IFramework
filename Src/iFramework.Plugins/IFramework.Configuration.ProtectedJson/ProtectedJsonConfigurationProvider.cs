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
        private readonly int _cipherPrefixSize;

        public ProtectedJsonConfigurationProvider(ProtectedJsonConfigurationSource source) : base(source)
        {
            _secretKey = GetEnvironmentVariable(source.SecretKeyName);
            _cipherPrefix = source.CipherPrefix;
            _cipherPrefixSize = _cipherPrefix.Length;
        }

        public override void Load(Stream stream)
        {
            base.Load(stream);

            var result = Data.Where(kv => kv.Value.StartsWith(_cipherPrefix)).ToList();
            if (result.Count > 0)
            {
                foreach (var item in result)
                {
                    var originalValue = Data[item.Key];
                    var value = originalValue[_cipherPrefixSize..]?.Trim();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new Exception($"Appsettings decrypt failed. key = {item.Key}, value = {originalValue}.");
                    }
                    try
                    {
                        Data[item.Key] = AesEncryptor.Decrypt(value, _secretKey);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Appsettings decrypt failed. key = {item.Key}, value = {originalValue}.", e);
                    }
                }
            }
        }

        private string GetEnvironmentVariable(string variable)
        {
            var value = Environment.GetEnvironmentVariable(variable)?.Trim();
            return value ?? throw new Exception($"Please set environment variable first. variable name is \"{variable}\"");
        }
    }
}
