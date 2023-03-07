using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    internal class SqliteUberRepository : DbContext, IUberRepository
    {
        public SqliteUberRepository(DbContextOptions<SqliteUberRepository> options) : base(options) { }

        public async Task<IReadOnlyCollection<T>> FindAllAsync<T>() where T : EntityBase
        {
            return await Set<T>().ToListAsync();
        }

        public async Task<IReadOnlyCollection<T>> FindAsync<T>(Expression<Func<T, bool>> expression) where T : EntityBase
        {
            Guard.Against.Null(expression);

            return await Set<T>().Where(expression).ToListAsync();
        }

        public async Task<T> AddAsync<T>(T entity) where T : EntityBase
        {
            Guard.Against.Null(entity);

            var e = await Set<T>().AddAsync(entity);
            await SaveChangesAsync();
            return e.Entity;
        }

        public async Task<T> UpdateAsync<T>(T entity) where T : EntityBase
        {
            Guard.Against.Null(entity);

            var e = Set<T>().Update(entity);
            await SaveChangesAsync();
            return e.Entity;
        }

        public async Task DeleteAsync<T>(T entity) where T : EntityBase
        {
            if (entity is not null)
            {
                Set<T>().Remove(entity);
                await SaveChangesAsync();
            }
        }
    }
}
