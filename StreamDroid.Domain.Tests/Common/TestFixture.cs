using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StreamDroid.Core.Entities;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Shared;

namespace StreamDroid.Domain.Tests.Common
{
    public sealed class TestFixture : IDisposable
    {
        private const string FilePath = "Common/appsettings.Test.json";

        private readonly ServiceProvider _serviceProvider;
        internal readonly IOptions<JwtSettings> options;
        internal readonly IRepository<User> userRepository;
        internal readonly IRepository<Reward> rewardRepository;
        internal readonly IRedemptionRepository redemptionRepository;

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

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInfrastructureConfiguration(configurationManager);
            _serviceProvider = serviceCollection.BuildServiceProvider();
            userRepository = _serviceProvider.GetRequiredService<IRepository<User>>();
            rewardRepository = _serviceProvider.GetRequiredService<IRepository<Reward>>();
            redemptionRepository = _serviceProvider.GetRequiredService<IRedemptionRepository>();
            options = Options.Create(jwtSettings);
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}
