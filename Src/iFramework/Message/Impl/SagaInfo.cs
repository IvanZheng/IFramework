using IFramework.Domain;

namespace IFramework.Message.Impl
{
    public class SagaInfo: ValueObject
    {
        public static SagaInfo Null => new SagaInfo();

        public string SagaId { get; set; }
        public string ReplyEndPoint { get; set; }
    }
}