namespace StreamDroid.Domain.Services.Stream.Events
{
    public class AssetEvent : EventBase
    {
        public int Volume { get; init; }
        public Uri? Uri { get; init; }

        public AssetEvent(EventType eventType) : base(eventType) { }
    }
}
