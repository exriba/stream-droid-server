namespace StreamDroid.Domain.Settings
{
    public class JwtSettings
    {
        public const string Key = "JwtSettings";

        public string SigningKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
    }
}
