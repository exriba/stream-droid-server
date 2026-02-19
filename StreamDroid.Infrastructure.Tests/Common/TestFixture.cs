using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Core.Entities;
using StreamDroid.Core.Interfaces;

namespace StreamDroid.Infrastructure.Tests.Common
{
    public sealed class TestFixture : IDisposable
    {
        private const string FilePath = "Common/appsettings.Test.json";

        private readonly ServiceProvider _serviceProvider;
        internal readonly IRepository<Reward> rewardRepository;
        internal readonly IRedemptionRepository redemptionRepository;

        public TestFixture()
        {
            using var configurationManager = new ConfigurationManager();
            configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile(FilePath)
                                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInfrastructureConfiguration(configurationManager);
            _serviceProvider = serviceCollection.BuildServiceProvider();
            rewardRepository = _serviceProvider.GetRequiredService<IRepository<Reward>>();
            redemptionRepository = _serviceProvider.GetRequiredService<IRedemptionRepository>();
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}
