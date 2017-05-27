using System.Text;

namespace Kafka.Client.Cfg
{
    public class BrokerConfiguration
    {
        public int BrokerId { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder(64);
            sb.AppendFormat("BrokerId={0},Host={1}:{2}", BrokerId, Host, Port);
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
                return false;

            // If parameter cannot be cast to Point return false.
            var p = obj as BrokerConfiguration;
            if (p == null)
                return false;

            // Return true if the fields match:
            return BrokerId == p.BrokerId && Host == p.Host && Port == p.Port;
        }

        public bool Equals(BrokerConfiguration p)
        {
            // If parameter is null return false:
            if (p == null)
                return false;

            // Return true if the fields match:
            return BrokerId == p.BrokerId && Host == p.Host && Port == p.Port;
        }

        public override int GetHashCode()
        {
            return BrokerId ^ Host.GetHashCode() ^ Port.GetHashCode();
        }

        public static string GetBrokerConfiturationString(int partitionIndex, BrokerConfiguration broker, bool isleader)
        {
            var sb = new StringBuilder();
            if (isleader)
                sb.AppendFormat("partition={0},leader[{0}]={1}", partitionIndex, broker);
            else
                sb.AppendFormat("partition={0},nonleader[{0}]={1}", partitionIndex, broker);
            return sb.ToString();
        }
    }
}