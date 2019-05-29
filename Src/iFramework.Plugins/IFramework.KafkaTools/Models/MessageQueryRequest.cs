namespace IFramework.KafkaTools.Models
{
    public class MessageQueryRequest
    {
        public string Broker { get; set; }
        public string Topic { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
    }
}