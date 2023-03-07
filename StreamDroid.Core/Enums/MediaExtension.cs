using Ardalis.SmartEnum;

namespace StreamDroid.Core.Enums
{
    public class MediaExtension : SmartEnum<MediaExtension>
    {
        public static readonly MediaExtension MP3 = new(nameof(MP3), 0);
        public static readonly MediaExtension MP4 = new(nameof(MP4), 1);

        protected MediaExtension(string name, int value) : base(name, value) {}

        public override string ToString() => $".{Name.ToLower()}";
    }
}
