namespace IFramework.Message
{
    /// <summary>
    /// 消息队列选项
    /// </summary>
    public class MessageQueueOptions
    {
        public MessageQueueOptions() { }

        public MessageQueueOptions(bool ensureArrival, bool ensureIdempotent, bool persistEvent)
        {
            EnsureArrival = ensureArrival;
            EnsureIdempotent = ensureIdempotent;
            PersistEvent = persistEvent;
        }

        /// <summary>
        /// 是否保证消息必达性, 在本地表中持久化未发送完毕的消息
        /// </summary>
        public bool EnsureArrival { get; set; } = true;

        /// <summary>
        /// 是否开启消费幂等性, 在本地表中记录消费者Id和消息Id(唯一索引)
        /// </summary>
        public bool EnsureIdempotent { get; set; } = true;

        /// <summary>
        /// 是否持久化事件
        /// </summary>
        public bool PersistEvent { get; set; } = true;
    }
}
