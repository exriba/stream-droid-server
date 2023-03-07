using Ardalis.SmartEnum;

namespace StreamDroid.Core.Enums
{
    public class MediaExtension : SmartEnum<MediaExtension>
    {
        public static readonly MediaExtension MP3 = new(nameof(MP3), "MP3", 0);
        public static readonly MediaExtension MP4 = new(nameof(MP4), "MP4", 1);

        public readonly string DisplayName;

        protected MediaExtension(string name, string displayName, int value) : base(name, value)
        {
            DisplayName = displayName;
        }

        public override string ToString() => $".{Name.ToLower()}";
    }
}
