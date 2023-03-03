using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StreamDroid.Domain.DTOs;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Infrastructure.Settings;
using StreamDroid.Shared;
using System.Reflection;

namespace StreamDroid.Domain.Tests.Common
{
    public abstract class TestFixture : IDisposable
    {
        private static bool Initialized;
        private readonly string _filePath;

        protected readonly LiteDbUberRepository _uberRepository;

        protected TestFixture(string databaseName)
        {
            if (!Initialized)
            {
                var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
                Assembly applicationAssembly = typeof(BaseDto<,>).Assembly;
                typeAdapterConfig.Scan(applicationAssembly);
                Initialized = true;
            }

            _filePath = @$"{Directory.GetCurrentDirectory()}/{databaseName}";
            var fileStream = new FileStream(_filePath, FileMode.Create);
            fileStream.Dispose();

            var liteDbSettings = new LiteDbSettings() { ConnectionString = $"Filename={_filePath}" };
            IOptions<LiteDbSettings> options = Options.Create(liteDbSettings);
            _uberRepository = new LiteDbUberRepository(options);

            using var configurationManager = new ConfigurationManager();
            configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Common/appsettings.Test.json")
                .Build();
            configurationManager.Configure();
        }

        public void Dispose()
        {
            _uberRepository.Dispose();
            File.Delete(_filePath);
        }
    }
}
