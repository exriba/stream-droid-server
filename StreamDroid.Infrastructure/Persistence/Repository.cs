using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    /// <summary>
    /// Default implementation of <see cref="IRepository{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity">Entity base class</typeparam>
    internal class Repository<TEntity> : IDisposable, IAsyncDisposable, IRepository<TEntity> where TEntity : EntityBase
    {
        private readonly DbSet<TEntity> _entitySet;
        private readonly DatabaseContext _databaseContext;

        public Repository(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
            _entitySet = databaseContext.Set<TEntity>();
        }

        /// <inheritdoc/>
        public async Task<TEntity?> FindByIdAsync(string id)
        {
            Guard.Against.NullOrWhiteSpace(id, nameof(id));

            return await _entitySet.FindAsync(id);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>>? expression = null)
        {
            return expression is null 
                ? await _entitySet.AsNoTracking().ToListAsync() 
                : await _entitySet.Where(expression).AsNoTracking().ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<TEntity> AddAsync(TEntity entity)
        {
            Guard.Against.Null(entity);

            var e = await _entitySet.AddAsync(entity);
            await _databaseContext.SaveChangesAsync();
            return e.Entity;
        }

        /// <inheritdoc/>
        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            Guard.Against.Null(entity);

            var e = _entitySet.Update(entity);
            await _databaseContext.SaveChangesAsync();
            return e.Entity;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id)
        {
            var entity = await FindByIdAsync(id);

            if (entity is not null)
            {
                _entitySet.Remove(entity);
                await _databaseContext.SaveChangesAsync();
            }
        }

        public void Dispose()
        {
            _databaseContext.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _databaseContext.DisposeAsync();
        }
    }
}
