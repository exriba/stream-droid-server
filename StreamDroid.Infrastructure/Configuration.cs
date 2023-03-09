using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamDroid.Core.Entities;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Infrastructure.Settings;

namespace StreamDroid.Infrastructure
{
    public static class Configuration
    {
        public static IServiceCollection AddInfrastructureConfiguration(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            // Add Configuration Options
            var sqliteSettings = new SqliteSettings();
            configurationManager.GetSection(SqliteSettings.Key).Bind(sqliteSettings);
            services.Configure<SqliteSettings>(configurationManager.GetSection(SqliteSettings.Key));

            // Add services to the container.
            services.AddDbContext<DatabaseContext>(options => options.UseSqlite(sqliteSettings.ConnectionString));
            services.AddScoped<IRepository<User>, Repository<User>>();
            services.AddScoped<IRepository<Reward>, Repository<Reward>>();
            services.AddScoped<IRepository<Redemption>, Repository<Redemption>>();
            return services;
        }
    }
}
