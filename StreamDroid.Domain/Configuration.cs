using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpTwitch.Auth;
using SharpTwitch.Core;
using SharpTwitch.EventSub;
using SharpTwitch.Helix;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Services.AssetFile;
using StreamDroid.Domain.Services.Stream;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Settings;
using System.Reflection;

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
            services.Configure<JwtSettings>(configurationManager.GetSection(JwtSettings.Key));

            var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
            Assembly applicationAssembly = typeof(BaseProto<,>).Assembly;
            typeAdapterConfig.Scan(applicationAssembly);

            // Add services to the container. 
            // services.AddMemoryCache(options => options.SizeLimit = 1024);
            // services.AddSingleton<IMemoryCache, MemoryCache>(); // Review
            services.AddTwitchEventSub();
            services.AddHttpClient();
            services.AddTwitchAuth();
            services.AddTwitchHelix();
            services.AddTwitchCore(configurationManager);
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAssetFileService, AssetFileService>();
            services.AddSingleton<NotificationService>();
            services.AddHostedService<TwitchEventSub>();
            services.AddSingleton<ITwitchSubscriber, TwitchEventSub>();
            return services;
        }
    }
}
