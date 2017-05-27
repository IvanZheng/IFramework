using System.Collections.Generic;
using Kafka.Client.Consumers;

namespace Kafka.Client.Requests
{
    /// <summary>
    ///     TODO: Update summary.
    /// </summary>
    public class FetchRequestBuilder
    {
        private readonly Dictionary<string, List<PartitionFetchInfo>> requestMap =
            new Dictionary<string, List<PartitionFetchInfo>>();

        private string clientId = string.Empty;
        private int correlationId = -1;
        private int maxWait = -1;
        private int minBytes = -1;

        public FetchRequestBuilder AddFetch(string topic, int partition, long offset, int fetchSize)
        {
            var fetchInfo = new PartitionFetchInfo(partition, offset, fetchSize);
            if (!requestMap.ContainsKey(topic))
            {
                var item = new List<PartitionFetchInfo> {fetchInfo};
                requestMap.Add(topic, item);
            }
            else
            {
                requestMap[topic].Add(fetchInfo);
            }
            return this;
        }

        public FetchRequestBuilder CorrelationId(int correlationId)
        {
            this.correlationId = correlationId;
            return this;
        }

        public FetchRequestBuilder ClientId(string clientId)
        {
            this.clientId = clientId;
            return this;
        }

        public FetchRequestBuilder MaxWait(int maxWait)
        {
            this.maxWait = maxWait;
            return this;
        }

        public FetchRequestBuilder MinBytes(int minBytes)
        {
            this.minBytes = minBytes;
            return this;
        }

        public FetchRequest Build()
        {
            return new FetchRequest(correlationId, clientId, maxWait, minBytes, requestMap);
        }
    }
}