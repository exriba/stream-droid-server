using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Core.Interfaces;
using StreamDroid.Shared;

namespace StreamDroid.Infrastructure.Tests.Common
{
    public sealed class TestFixture : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        internal readonly IUberRepository repository;

        public TestFixture()
        {
            var dictionary = new Dictionary<string, string>
            {
                { "EncryptionSettings:KeyPhrase", "w9z$C&F)H@McQfTj" },
                { "SqliteSettings:ConnectionString", "Data Source=file::memory:?cache=shared" }
            };

            using var configurationManager = new ConfigurationManager();
            configurationManager.AddInMemoryCollection(dictionary!).Build();
            configurationManager.Configure();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInfrastructureConfiguration(configurationManager);

            _serviceProvider = serviceCollection.BuildServiceProvider();
            repository = _serviceProvider.GetRequiredService<IUberRepository>();
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}
