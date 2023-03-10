using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Services.Reward;
using SharpTwitch.Auth;
using SharpTwitch.Helix;
using Mapster;
using StreamDroid.Domain.DTOs;
using System.Reflection;
using StreamDroid.Domain.Services.Stream;

namespace StreamDroid.Domain
{
    public static class Configuration
    {
        public static IServiceCollection AddServiceConfiguration(this IServiceCollection services)
        {
            var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
            Assembly applicationAssembly = typeof(BaseDto<,>).Assembly;
            typeAdapterConfig.Scan(applicationAssembly);

            // Add services to the container. 
            // services.AddMemoryCache(options => options.SizeLimit = 1024);
            // services.AddSingleton<IMemoryCache, MemoryCache>(); // Review
            services.AddTwitchAuth();
            services.AddTwitchHelix();
            services.AddSingleton<ITwitchEventSub, TwitchEventSub>();
            services.AddScoped<IRewardService, RewardService>();
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
