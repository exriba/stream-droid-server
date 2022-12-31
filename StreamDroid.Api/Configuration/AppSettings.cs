using StreamDroid.Domain.Configuration;

namespace StreamDroid.Application.Configuration
{
    internal class AppSettings : IAppSettings
    {
        public const string Key = "AppSettings";

        public string ClientUri { get; set; } = string.Empty;
        public string StaticAssetPath { get; set; } = string.Empty;
        public string StaticAssetUri { get; set; } = string.Empty;
    }
}
