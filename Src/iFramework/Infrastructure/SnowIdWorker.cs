using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using IFramework.Config;
using Microsoft.Extensions.Configuration;
using Snowflake.Core;

namespace IFramework.Infrastructure
{
    public class SnowIdWorker
    {
        private static Lazy<IdWorker> Instance = new Lazy<IdWorker>(() =>
        {
            var workerId = Configuration.Instance.GetValue<long?>($"{nameof(SnowIdWorker)}:Id") ?? 0;
            var dataCenterId = Configuration.Instance.GetValue<long?>($"{nameof(SnowIdWorker)}:DataCenterId") ?? 0;
            var (workerIdFromIp, dataCenterIdFromIp) = GenerateIdsFromIp(GetLocalIpAddress());
            if (workerId == 0)
            {
                workerId = workerIdFromIp;
            }

            if (dataCenterId == 0)
            {
                dataCenterId = dataCenterIdFromIp;
            }

            return new IdWorker(workerId, dataCenterId);
        });

        public static string GenerateId()
        {
            return Instance.Value.NextId().ToString();
        }

        private static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static (long workerId, long datacenterId) GenerateIdsFromIp(string ipAddress)
        {
            var ipParts = ipAddress.Split('.').Select(int.Parse).ToArray();
            long workerId = ipParts[2] % 32; // 使用第三段生成 workerId
            long datacenterId = ipParts[3] % 32; // 使用第四段生成 datacenterId
            return (workerId, datacenterId);
        }
    }
}
