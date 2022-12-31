using Microsoft.Extensions.Configuration;
using StreamDroid.Shared.Configuration;
using StreamDroid.Shared.Helpers;

namespace StreamDroid.Shared
{
    public static class Extensions
    {
        public static void Configure(this ConfigurationManager configurationManager)
        {
            var encryptionSettings = new EncryptionSettings();
            configurationManager.GetSection(EncryptionSettings.Key).Bind(encryptionSettings);
            EncryptionExtensions.Configure(encryptionSettings);
        }
    }
}
