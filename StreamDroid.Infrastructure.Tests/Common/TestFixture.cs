using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Core.Interfaces;

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
                { "SqliteSettings:ConnectionString", "Data Source=file::memory:?cache=shared" }
            };

            using var configurationManager = new ConfigurationManager();
            configurationManager.AddInMemoryCollection(dictionary!).Build();

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
