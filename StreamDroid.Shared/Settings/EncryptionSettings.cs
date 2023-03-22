namespace StreamDroid.Shared.Settings
{
    /// <summary>
    /// Encryption Settings POCO.
    /// </summary>
    internal class EncryptionSettings
    {
        public const string Key = "EncryptionSettings";

        public string KeyPhrase { get; set; } = string.Empty;
    }
}
