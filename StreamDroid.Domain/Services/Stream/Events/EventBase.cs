namespace StreamDroid.Domain.Services.Stream.Events
{
    public abstract class EventBase
    {
        public string Id { get; init; } = string.Empty;

        public string EventType { get; } = string.Empty;

        protected EventBase(EventType eventType)
        {
            Id = Guid.NewGuid().ToString();
            EventType = eventType.ToString();
        }
    }
}
