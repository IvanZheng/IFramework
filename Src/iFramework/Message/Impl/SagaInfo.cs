using IFramework.Domain;

namespace IFramework.Message.Impl
{
    public class SagaInfo: ValueObject<SagaInfo>
    {
        public static SagaInfo Null => new SagaInfo();

        public string SagaId { get; set; }
        public string ReplyEndPoint { get; set; }
    }
}