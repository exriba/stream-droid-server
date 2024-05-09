using Ardalis.SmartEnum;

namespace StreamDroid.Domain.Services.Stream.Events
{
    public class EventType : SmartEnum<EventType>
    {
        /// <summary>
        /// MP3 extension
        /// </summary>
        public static readonly EventType AUDIO = new(nameof(AUDIO), 0);

        /// <summary>
        /// MP4 extension
        /// </summary>
        public static readonly EventType VIDEO = new(nameof(VIDEO), 1);


        public static readonly EventType SPEECH = new(nameof(SPEECH), 2);

        protected EventType(string name, int value) : base(name, value) { }

        public override string ToString() => Name;
    }
}
