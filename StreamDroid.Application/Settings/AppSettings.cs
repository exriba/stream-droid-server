using StreamDroid.Domain.Settings;

namespace StreamDroid.Application.Settings
{
    /// <summary>
    /// App Settings POCO.
    /// </summary>
    public class AppSettings : IAppSettings
    {
        public const string Key = "AppSettings";

        public string ClientUri { get; set; } = string.Empty;
        public string StaticAssetPath { get; set; } = string.Empty;
        public string StaticAssetUri { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
    }
}
