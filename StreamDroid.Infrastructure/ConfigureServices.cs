using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Infrastructure.Persistence;

namespace StreamDroid.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureConfiguration(this IServiceCollection services)
        {
            // Add services to the container.
            services.AddScoped<IUberRepository, UberRepository>();
            return services;
        }
    }
}
