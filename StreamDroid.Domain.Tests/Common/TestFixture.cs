using Mapster;
using Microsoft.Extensions.Configuration;
using Moq;
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
        private readonly ConfigurationManager _configurationManager;

        protected readonly UberRepository _uberRepository;

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

            var persistenceSettings = new Mock<IPersistenceSettings>();
            persistenceSettings.Setup(x => x.ConnectionString).Returns($"Filename={_filePath}");
            _uberRepository = new UberRepository(persistenceSettings.Object);

            _configurationManager = new ConfigurationManager();
            _configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Common/appsettings.Test.json")
                .Build();
            _configurationManager.Configure();
        }

        public void Dispose()
        {
            _uberRepository.Dispose();
            _configurationManager.Dispose();
            File.Delete(_filePath);
        }
    }
}
