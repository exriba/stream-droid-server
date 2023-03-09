using Ardalis.GuardClauses;
using Ardalis.SmartEnum;

namespace StreamDroid.Core.Enums
{
    public class MediaExtension : SmartEnum<MediaExtension>
    {
        public static readonly MediaExtension MP3 = new(nameof(MP3), 0);
        public static readonly MediaExtension MP4 = new(nameof(MP4), 1);

        private static readonly IDictionary<string, MediaExtension> MediaExtensions = new Dictionary<string, MediaExtension>
        {
            { MP3.ToString(), MP3 },
            { MP4.ToString(), MP4 },
        };

        protected MediaExtension(string name, int value) : base(name, value) {}

        public static MediaExtension FromName(string name)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));

            if (!MediaExtensions.TryGetValue(name, out var value))
                throw new ArgumentException($"Invalid Media Extension {name}.");
            
            return value;
        }

        public override string ToString() => $".{Name.ToLower()}";
    }
}
