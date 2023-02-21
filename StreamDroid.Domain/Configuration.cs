using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Services.Reward;
using SharpTwitch.Auth;
using SharpTwitch.Helix;

namespace StreamDroid.Domain
{
    public static class Configuration
    {
        public static IServiceCollection AddServiceConfiguration(this IServiceCollection services)
        {
            // Add services to the container. 
            // Review
            // services.AddMemoryCache(options => options.SizeLimit = 1024);
            // services.AddSingleton<IMemoryCache, MemoryCache>(); // Review
            services.AddTwitchAuth();
            services.AddTwitchHelix();
            services.AddScoped<IRewardService, RewardService>();
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
