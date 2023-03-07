using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    internal class SqliteUberRepository : DbContext, IUberRepository
    {
        public SqliteUberRepository(DbContextOptions<SqliteUberRepository> options) : base(options) { }

        public async Task<IReadOnlyCollection<T>> FindAll<T>() where T : EntityBase
        {
            return await Set<T>().ToListAsync();
        }

        public async Task<IReadOnlyCollection<T>> Find<T>(Expression<Func<T, bool>> expression) where T : EntityBase
        {
            Guard.Against.Null(expression);

            return await Set<T>().Where(expression).ToListAsync();
        }

        public async Task<T> Save<T>(T entity) where T : EntityBase
        {
            Guard.Against.Null(entity);

            var dbSet = Set<T>();
            var exists = await dbSet.ContainsAsync(entity);

            if (exists)
            {
                var ue = dbSet.Update(entity);
                await SaveChangesAsync();
                return ue.Entity;
            }

            var ne = await dbSet.AddAsync(entity);
            await SaveChangesAsync();
            return ne.Entity;
        }

        public async Task Delete<T>(T entity) where T : EntityBase
        {
            Guard.Against.Null(entity);

            Set<T>().Remove(entity);
            await SaveChangesAsync();
        }
    }
}
