using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Settings;
using StreamDroid.Shared;

namespace StreamDroid.Domain.Tests.Common
{
    public sealed class TestFixture
    {
        internal readonly IOptions<JwtSettings> options;

        public TestFixture()
        {
            var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
            var applicationAssembly = typeof(BaseProto<,>).Assembly;
            typeAdapterConfig.Scan(applicationAssembly);

            var dictionary = new Dictionary<string, string>
            {
                { "EncryptionSettings:KeyPhrase", "w9z$C&F)H@McQfTj" },
                { "JwtSettings:SigningKey", "this-is-a-super-secret-signingkey-please-dont-steal-it" },
                { "JwtSettings:Issuer", "stream-droid-server" },
                { "JwtSettings:Audience", "stream-droid-client" }
            };

            using var configurationManager = new ConfigurationManager();
            configurationManager.AddInMemoryCollection(dictionary).Build();
            configurationManager.Configure();

            var jwtSettings = new JwtSettings();
            configurationManager.GetSection(JwtSettings.Key).Bind(jwtSettings);
            options = Options.Create(jwtSettings);
        }
    }
}
