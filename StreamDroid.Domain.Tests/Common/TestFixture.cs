using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Domain.DTOs;
using StreamDroid.Infrastructure;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Shared;
using StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Tests.Common
{
    public sealed class TestFixture : IDisposable
    {
        private const string FilePath = "Common/appsettings.Test.json";

        private readonly ServiceProvider _serviceProvider;
        internal readonly IRepository<User> userRepository;
        internal readonly IRepository<Reward> rewardRepository;
        internal readonly IRedemptionRepository redemptionRepository;

        public TestFixture()
        {
            var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
            var applicationAssembly = typeof(BaseDto<,>).Assembly;
            typeAdapterConfig.Scan(applicationAssembly);

            using var configurationManager = new ConfigurationManager();
            configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile(FilePath)
                                .Build();
            configurationManager.Configure();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInfrastructureConfiguration(configurationManager);
            _serviceProvider = serviceCollection.BuildServiceProvider();
            userRepository = _serviceProvider.GetRequiredService<IRepository<User>>();
            rewardRepository = _serviceProvider.GetRequiredService<IRepository<Reward>>();
            redemptionRepository = _serviceProvider.GetRequiredService<IRedemptionRepository>();
        }

        public void Dispose()
        {
            redemptionRepository.Dispose();
            userRepository.Dispose();
            rewardRepository.Dispose();
            _serviceProvider.Dispose();
        }
    }
}
