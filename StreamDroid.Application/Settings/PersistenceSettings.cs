using StreamDroid.Infrastructure.Settings;

namespace StreamDroid.Application.Settings
{
    internal class PersistenceSettings : IPersistenceSettings
    {
        public const string Key = "PersistenceSettings";

        public string ConnectionString { get; set; } = string.Empty;
    }
}
