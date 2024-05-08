using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Services.Reward;
using SharpTwitch.Auth;
using SharpTwitch.Helix;
using Mapster;
using StreamDroid.Domain.DTOs;
using System.Reflection;
using StreamDroid.Domain.Services.Stream;
using StreamDroid.Domain.Services.Redemption;
using SharpTwitch.Core;
using Microsoft.Extensions.Configuration;
using StreamDroid.Domain.Services.Data;

namespace StreamDroid.Domain
{
    /// <summary>
    /// StreamDroid.Domain Configuration.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Configures the business domain services.
        /// </summary>
        /// <param name="services">service collection</param>
        /// <param name="configurationManager">configuration manager</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddServiceConfiguration(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
            Assembly applicationAssembly = typeof(BaseDto<,>).Assembly;
            typeAdapterConfig.Scan(applicationAssembly);

            // Add services to the container. 
            // services.AddMemoryCache(options => options.SizeLimit = 1024);
            // services.AddSingleton<IMemoryCache, MemoryCache>(); // Review
            services.AddTwitchAuth();
            services.AddTwitchHelix();
            services.AddTwitchCore(configurationManager);
            services.AddSingleton<ITwitchEventSub, TwitchEventSub>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRewardService, RewardService>();
            services.AddScoped<IRedemptionService, RedemptionService>();
            services.AddScoped<IDataService, DataService>();
            services.AddHostedService<TwitchEventSub>();
            return services;
        }
    }
}
