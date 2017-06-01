using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace Kafka.Client.Consumers
{
    internal class TopicCount
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(TopicCount));
        private readonly string consumerIdString;

        private readonly IDictionary<string, int> topicCountMap;

        public TopicCount(string consumerIdString, IDictionary<string, int> topicCountMap)
        {
            this.topicCountMap = topicCountMap;
            this.consumerIdString = consumerIdString;
        }

        public static TopicCount ConstructTopicCount(string consumerIdString, string json)
        {
            Dictionary<string, int> result = null;
            var ser = new JavaScriptSerializer();
            try
            {
                result = ser.Deserialize<Dictionary<string, int>>(json);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("error parsing consumer json string {0}. {1}", json, ex.FormatException());
            }

            return new TopicCount(consumerIdString, result);
        }

        public IDictionary<string, IList<string>> GetConsumerThreadIdsPerTopic()
        {
            var result = new Dictionary<string, IList<string>>();
            foreach (var item in topicCountMap)
            {
                var consumerSet = new List<string>();
                for (var i = 0; i < item.Value; i++)
                {
                    consumerSet.Add(consumerIdString + "-" + i);
                }

                result.Add(item.Key, consumerSet);
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            var o = obj as TopicCount;
            if (o != null)
            {
                return consumerIdString == o.consumerIdString && topicCountMap == o.topicCountMap;
            }

            return false;
        }


        public override int GetHashCode()
        {
            if (consumerIdString != null && topicCountMap != null)
            {
                return consumerIdString.GetHashCode() ^ topicCountMap.GetHashCode();
            }
            if (consumerIdString != null)
            {
                return consumerIdString.GetHashCode();
            }
            if (topicCountMap != null)
            {
                return topicCountMap.GetHashCode();
            }
            return 0;
        }

        /*
         return json of
         { "topic1" : 4,
           "topic2" : 4
         }
        */
        public string ToJsonString()
        {
            var sb = new StringBuilder();
            sb.Append("{ ");
            var i = 0;
            foreach (var entry in topicCountMap)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }

                sb.Append("\"" + entry.Key + "\": " + entry.Value);
                i++;
            }

            sb.Append(" }");
            return sb.ToString();
        }
    }
}