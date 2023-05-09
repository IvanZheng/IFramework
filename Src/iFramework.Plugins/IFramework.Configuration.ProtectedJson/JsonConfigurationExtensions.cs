using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace IFramework.Configuration.ProtectedJson
{
    public static class JsonConfigurationExtensions
    {
        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, HostBuilderContext hostingContext, bool reloadOnChange = true, string secretKeyEnvironmentVariableName = "PROTECTEDJSON_SECRET_KEY", string cipherPrefix = "cipherText:")
        {
            IHostEnvironment env = hostingContext.HostingEnvironment;
            builder
                .AddProtectedJsonFile("appsettings.json", true, reloadOnChange, secretKeyEnvironmentVariableName, cipherPrefix)
                .AddProtectedJsonFile($"appsettings.{env.EnvironmentName}.json", true, reloadOnChange, secretKeyEnvironmentVariableName, cipherPrefix);
            return builder;
        }


        public static IConfigurationBuilder AddProtectedJsonFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange, string secretKeyEnvironmentVariableName = "PROTECTEDJSON_SECRET_KEY", string cipherPrefix = "cipherText:")
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("File path must be a non-empty string.");
            }

            var source = new ProtectedJsonConfigurationSource
            {
                FileProvider = null,
                Path = path,
                Optional = optional,
                ReloadOnChange = reloadOnChange,
                SecretKeyName = secretKeyEnvironmentVariableName,
                CipherPrefix = cipherPrefix,
            };

            source.ResolveFileProvider();
            builder.Add(source);
            return builder;
        }
    }

}
