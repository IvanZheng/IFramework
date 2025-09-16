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
        public static readonly DateTime Epoch = ConvertTwepochToDateTime(IdWorker.Twepoch);

        public static DateTime ConvertTwepochToDateTime(long twepoch)
        {
            // Unix epoch 起点
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // 加上 Twepoch 毫秒数
            return unixEpoch.AddMilliseconds(twepoch);
        }

        /// <summary>
        /// 获取生成器ID，支持多种方式
        /// 优先级: 环境变量 > 配置文件 > 主机名哈希 > 默认值
        /// </summary>
        private static (long, long) GetGeneratorId()
        {
            // 使用Pod名称或主机名生成唯一ID（适用于K8s部署）
            var podName = Environment.GetEnvironmentVariable("HOSTNAME") ??
                          Environment.GetEnvironmentVariable("POD_NAME") ??
                          Environment.MachineName;

            if (!string.IsNullOrEmpty(podName))
            {
                // 使用主机名/Pod名称的哈希值来生成GeneratorId
                var hashCode = Math.Abs(podName.GetHashCode());
                var hostBasedId = hashCode % 1024;
                //logger.LogInformation("Using GeneratorId based on hostname/pod name '{PodName}': {GeneratorId}", podName, hostBasedId);
                int high5 = (hostBasedId >> 5) & 0b11111; // 取高5位
                int low5 = hostBasedId & 0b11111;         // 取低5位
                return (high5, low5);
            }

            // 4. 默认值
            //logger.LogWarning("Using default GeneratorId: 1. Consider setting SNOWFLAKE_GENERATOR_ID environment variable for production deployment");
            return (0, 0);
        }

        public static Lazy<IdWorker> Instance = new Lazy<IdWorker>(() =>
        {
            var workerId = Configuration.Instance.GetValue<long?>($"{nameof(SnowIdWorker)}:Id") ?? 0;
            var dataCenterId = Configuration.Instance.GetValue<long?>($"{nameof(SnowIdWorker)}:DataCenterId") ?? 0;

            var (workerIdFromName, dataCenterIdFromName) = GetGeneratorId();

            if (workerId == 0)
            {
                workerId = workerIdFromName;
            }

            if (dataCenterId == 0)
            {
                dataCenterId = dataCenterIdFromName;
            }
        
            if (workerId == 0 && dataCenterId == 0)
            {
                var (workerIdFromIp, dataCenterIdFromIp) = GenerateIdsFromIp(GetLocalIpAddress());
                workerId = workerIdFromIp;
                dataCenterId = dataCenterIdFromIp;
            }

            return new IdWorker(workerId, dataCenterId);
        });

        public static string GenerateId()
        {
            return Instance.Value.NextId().ToString();
        }

        public static long GenerateLongId()
        {
            return Instance.Value.NextId();
        }

        public static DateTime ExtractTimestamp(string snowflakeId)
        {
            if (long.TryParse(snowflakeId, out var timestamp))
            {
                return ExtractTimestamp(timestamp);
            }
            return DateTime.MinValue;
        }

        public static DateTime ExtractTimestamp(long snowflakeId)
        {
            // 右移 22 位，得到 41 位的时间戳部分
            long timestampPart = (snowflakeId >> 22);
            // 加上 epoch 得到实际时间
            return Epoch.AddMilliseconds(timestampPart).ToLocalTime();
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
