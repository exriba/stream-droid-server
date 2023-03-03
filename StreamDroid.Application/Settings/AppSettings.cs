namespace StreamDroid.Application.Settings
{
    public class AppSettings
    {
        public const string Key = "AppSettings";

        public string ClientUri { get; set; } = string.Empty;
        public string StaticAssetPath { get; set; } = string.Empty;
        public string StaticAssetUri { get; set; } = string.Empty;
    }
}
