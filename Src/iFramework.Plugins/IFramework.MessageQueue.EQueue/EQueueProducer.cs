using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EQueue.Clients.Producers;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueProducer
    {
        private readonly ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(EQueueProducer).Name);

        public EQueueProducer(string clusterName, List<IPEndPoint> nameServerList)
        {
            ClusterName = clusterName;
            NameServerList = nameServerList;
        }

        public Producer Producer { get; protected set; }
        public string ClusterName { get; protected set; }
        public List<IPEndPoint> NameServerList { get; protected set; }
        public int AdminPort { get; protected set; }

        public void Start()
        {
            var setting = new ProducerSetting
            {
                ClusterName = ClusterName,
                NameServerList = NameServerList
            };
            Producer = new Producer(setting).Start();
        }

        public void Stop()
        {
            Producer?.Shutdown();
        }

        public async Task SendAsync(global::EQueue.Protocols.Message equeueMessage, string key, CancellationToken cancellationToken)
        {
            if (key == null)
            {
                key = string.Empty;
            }

            var retryTimes = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                retryTimes++;
                // 每次发送失败后线性增长等待发送时间 如: 5s, 10s, 15s, 20s .... max:5 minutes
                var waitTime = Math.Min(retryTimes * 1000 * 5, 60000 * 5);
                try
                {
                    var result = await Producer.SendAsync(equeueMessage, key)
                                                .ConfigureAwait(false);
                    if (result.SendStatus != SendStatus.Success)
                    {
                        _logger.Error($"send message failed topic: {equeueMessage.Topic} key:{key} error:{result.ErrorMessage}");
                        await Task.Delay(waitTime, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"send message failed topic: {equeueMessage.Topic} key:{key}", e);
                    await Task.Delay(waitTime, cancellationToken);
                }
            }
        }
    }
}