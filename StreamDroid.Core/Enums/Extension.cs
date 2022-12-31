namespace StreamDroid.Core.Enums
{
    public enum Extension
    {
        MP3,
        MP4
    }

    public static class Extensions
    {
        public static string GetExtension(this Extension mediaType)
        {
            return mediaType switch
            {
                Extension.MP3 => ".mp3",
                Extension.MP4 => ".mp4",
                _ => throw new ArgumentException("Invalid Extension.", nameof(mediaType)),
            };
        }
    }
}
