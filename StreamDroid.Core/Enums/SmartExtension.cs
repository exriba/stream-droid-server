using Ardalis.SmartEnum;

namespace StreamDroid.Core.Enums
{
    public class SmartExtension : SmartEnum<SmartExtension>
    {
        public static readonly SmartExtension MP3 = new(nameof(MP3), "MP3", 0);
        public static readonly SmartExtension MP4 = new(nameof(MP4), "MP4", 1);

        public readonly string DisplayName;

        protected SmartExtension(string name, string displayName, int value) : base(name, value)
        {
            DisplayName = displayName;
        }

        public override string ToString() => $".{Name.ToLower()}";
    }
}
