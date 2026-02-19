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
        private const string FilePath = "Common/appsettings.Test.json";

        internal readonly IOptions<JwtSettings> options;

        public TestFixture()
        {
            var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
            var applicationAssembly = typeof(BaseProto<,>).Assembly;
            typeAdapterConfig.Scan(applicationAssembly);

            using var configurationManager = new ConfigurationManager();
            configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile(FilePath)
                                .Build();
            configurationManager.Configure();

            var jwtSettings = new JwtSettings();
            configurationManager.GetSection(JwtSettings.Key).Bind(jwtSettings);
            options = Options.Create(jwtSettings);
        }
    }
}
