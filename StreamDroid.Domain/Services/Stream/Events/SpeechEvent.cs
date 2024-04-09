namespace StreamDroid.Domain.Services.Stream.Events
{
    public class SpeechEvent : EventBase
    {
        public int VoiceIndex { get; init; }
        public string Message { get; init; } = string.Empty;

        public SpeechEvent() : base(Events.EventType.SPEECH) { }
    }
}
