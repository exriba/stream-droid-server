using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Core.Interfaces;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Infrastructure.Settings;

namespace StreamDroid.Infrastructure
{
    /// <summary>
    /// StreamDroid.Infrastructure Configuration.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Configures the database context and repositories.
        /// </summary>
        /// <param name="services">service collection</param>
        /// <param name="configurationManager">configuration manager</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddInfrastructureConfiguration(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            // Add Configuration Options
            var sqliteSettings = new SqliteSettings();
            configurationManager.GetSection(SqliteSettings.Key).Bind(sqliteSettings);
            services.Configure<SqliteSettings>(configurationManager.GetSection(SqliteSettings.Key));

            // Add services to the container.
            services.AddDbContext<DatabaseContext>(options => options.UseSqlite(sqliteSettings.ConnectionString));
            services.AddScoped<IUberRepository, UberRepository>();
            return services;
        }
    }
}
