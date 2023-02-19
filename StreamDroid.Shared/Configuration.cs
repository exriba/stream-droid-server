using Microsoft.Extensions.Configuration;
using StreamDroid.Shared.Extensions;
using StreamDroid.Shared.Settings;

namespace StreamDroid.Shared
{
    public static class Configuration
    {
        public static void Configure(this ConfigurationManager configurationManager)
        {
            var encryptionSettings = new EncryptionSettings();
            configurationManager.GetSection(EncryptionSettings.Key).Bind(encryptionSettings);
            EncryptionExtensions.Configure(encryptionSettings);
        }
    }
}
