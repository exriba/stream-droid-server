using Ardalis.SmartEnum;

namespace StreamDroid.Core.Enums
{
    /// <summary>
    /// Media extension enum. 
    /// </summary>
    public class MediaExtension : SmartEnum<MediaExtension>
    {
        /// <summary>
        /// MP3 extension
        /// </summary>
        public static readonly MediaExtension MP3 = new(nameof(MP3), 0);
        
        /// <summary>
        /// MP4 extension
        /// </summary>
        public static readonly MediaExtension MP4 = new(nameof(MP4), 1);

        protected MediaExtension(string name, int value) : base(name, value) {}

        public override string ToString() => $".{Name.ToLower()}";
    }
}
