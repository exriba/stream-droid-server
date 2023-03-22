using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace StreamDroid.Infrastructure.Persistence
{
    /// <summary>
    /// Database context.
    /// </summary>
    internal class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
