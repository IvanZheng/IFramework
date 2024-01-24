using IFramework.Domain;

namespace IFramework.Message.Impl
{
    #if !NET5_0_OR_GREATER

    public class SagaInfo: ValueObject
    #else
    public record SagaInfo: ValueObject
    #endif
    {
        public static SagaInfo Null => new SagaInfo();

        public string SagaId { get; set; }
        public string ReplyEndPoint { get; set; }
        public override bool IsNull()
        {
            return string.IsNullOrWhiteSpace(SagaId);
        }
    }
}