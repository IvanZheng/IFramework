namespace IFramework.Event
{
    public interface IAggregateRootEvent : IEvent
    {
        object AggregateRootId { get; }
        string AggregateRootName { get; set; }
        int Version { get; set; }
    }
}