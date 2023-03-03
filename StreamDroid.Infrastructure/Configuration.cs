using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Infrastructure.Settings;
using StreamDroid.Infrastructure.Persistence;

namespace StreamDroid.Infrastructure
{
    public static class Configuration
    {
        public static IServiceCollection AddInfrastructureConfiguration(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            // Add Configuration Options
            services.Configure<LiteDbSettings>(configurationManager.GetSection(LiteDbSettings.Key));

            // Add services to the container.
            services.AddScoped<IUberRepository, LiteDbUberRepository>();
            return services;
        }
    }
}
