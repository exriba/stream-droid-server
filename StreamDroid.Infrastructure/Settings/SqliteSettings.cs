namespace StreamDroid.Infrastructure.Settings
{
    /// <summary>
    /// Sqlite settings POCO.
    /// </summary>
    public class SqliteSettings
    {
        public const string Key = "SqliteSettings";

        public string ConnectionString { get; set; } = string.Empty;
    }
}
