namespace StreamDroid.Shared.Configuration
{
    internal class EncryptionSettings
    {
        public const string Key = "EncryptionSettings";

        public string KeyPhrase { get; set; } = string.Empty;
    }
}
