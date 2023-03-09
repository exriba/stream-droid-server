namespace StreamDroid.Infrastructure.Settings
{
    public class SqliteSettings
    {
        public const string Key = "SqliteSettings";

        public string ConnectionString { get; set; } = string.Empty;
    }
}
