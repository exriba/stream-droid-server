using Microsoft.Extensions.Configuration;
using StreamDroid.Shared.Extensions;
using StreamDroid.Shared.Settings;

namespace StreamDroid.Shared
{
    /// <summary>
    /// StreamDroid.Shared Configuration.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Configures the encryption extensions using the encryption settings defined in the configuration. 
        /// </summary>
        /// <param name="configurationManager">configuration manager</param>
        public static void Configure(this ConfigurationManager configurationManager)
        {
            var encryptionSettings = new EncryptionSettings();
            configurationManager.GetSection(EncryptionSettings.Key).Bind(encryptionSettings);
            EncryptionExtensions.Configure(encryptionSettings);
        }
    }
}
