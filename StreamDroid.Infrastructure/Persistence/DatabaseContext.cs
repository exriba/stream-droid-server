using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace StreamDroid.Infrastructure.Persistence
{
    internal class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions options) : base(options)
        {            
            if (!Database.EnsureCreated())
                Database.Migrate();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
