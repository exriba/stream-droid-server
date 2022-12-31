using StreamDroid.Infrastructure.Configuration;

namespace StreamDroid.Application.Configuration
{
    internal class PersistenceSettings : IPersistenceSettings
    {
        public const string Key = "PersistenceSettings";

        public string ConnectionString { get; set; } = string.Empty;
    }
}
