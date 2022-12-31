using Microsoft.Extensions.DependencyInjection;
using SharpTwitch.Core.Interfaces;
using SharpTwitch.Core;
using StreamDroid.Domain.Reward;
using Microsoft.Extensions.Caching.Memory;
using StreamDroid.Domain.User;

namespace StreamDroid.Domain
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddServiceConfiguration(this IServiceCollection services)
        {
            // Add services to the container.
            services.AddMemoryCache(options => options.SizeLimit = 1024); // Review
            services.AddSingleton<IMemoryCache, MemoryCache>(); // Review
            services.AddSingleton<IApiCore, DefaultApiCore>();
            services.AddScoped<IRewardService, RewardService>();
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
