using System;
using System.IO;

namespace IFramework.Infrastructure
{
    internal static class RedisPodIdentity
    {
        internal static string FromEnvironment(string scope)
        {
            var pod = Environment.GetEnvironmentVariable("POD_NAME");
            if (string.IsNullOrWhiteSpace(pod)) pod = Environment.GetEnvironmentVariable("HOSTNAME");
            if (string.IsNullOrWhiteSpace(pod))
                throw new InvalidOperationException("Snowflake Pod binding requires POD_NAME or HOSTNAME.");

            if (string.IsNullOrWhiteSpace(scope)) scope = Environment.GetEnvironmentVariable("POD_NAMESPACE");
            const string namespaceFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
            if (string.IsNullOrWhiteSpace(scope) && File.Exists(namespaceFile))
                scope = File.ReadAllText(namespaceFile).Trim();
            if (string.IsNullOrWhiteSpace(scope))
                throw new InvalidOperationException("Configure SnowIdWorker:PodScope or supply POD_NAMESPACE.");

            return Uri.EscapeDataString(scope) + "/" + Uri.EscapeDataString(pod);
        }
    }
}
