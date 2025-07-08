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


        public static Lazy<IdWorker> Instance = new Lazy<IdWorker>(() =>
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
