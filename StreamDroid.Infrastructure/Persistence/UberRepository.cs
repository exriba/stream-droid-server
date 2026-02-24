using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using StreamDroid.Core.Common;
using StreamDroid.Core.Interfaces;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace StreamDroid.Infrastructure.Persistence
{
    internal class UberRepository : IUberRepository, IDisposable, IAsyncDisposable
    {
        private readonly DatabaseContext _databaseContext;

        public UberRepository(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        /// <inheritdoc/>
        public async Task<TEntity?> FindByIdAsync<TEntity>(string id, CancellationToken cancellationToken = default) where TEntity : EntityBase
        {
            Guard.Against.NullOrWhiteSpace(id, nameof(id));

            var entitySet = _databaseContext.Set<TEntity>();
            return await entitySet.FindAsync(id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<long> CountAsync<TEntity>(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellationToken = default) where TEntity : EntityBase
        {
            var entitySet = _databaseContext.Set<TEntity>();
            var query = entitySet.AsQueryable();

            if (expression is not null)
                query = query.Where(expression);

            return await query.LongCountAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<TEntity> FindStreamAsync<TEntity>(Expression<Func<TEntity, bool>>? expression = null, int? offset = null, int? limit = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TEntity : EntityBase
        {
            var query = CreateQuery(expression, offset, limit);

            await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return entity;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<TEntity>> FindListAsync<TEntity>(Expression<Func<TEntity, bool>>? expression = null, int? offset = null, int? limit = null, CancellationToken cancellationToken = default) where TEntity : EntityBase
        {
            var query = CreateQuery(expression, offset, limit);

            return await query.ToListAsync(cancellationToken);
        }

        private IQueryable<TEntity> CreateQuery<TEntity>(Expression<Func<TEntity, bool>>? expression = null, int? offset = null, int? limit = null) where TEntity : EntityBase
        {
            var entitySet = _databaseContext.Set<TEntity>();
            var query = entitySet.AsNoTracking();

            if (expression is not null)
                query = query.Where(expression);

            if (offset.HasValue)
                query = query.Skip(offset.Value);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return query;
        }

        /// <inheritdoc/>
        public async Task<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : EntityBase
        {
            Guard.Against.Null(entity);

            var entitySet = _databaseContext.Set<TEntity>();
            var entry = await entitySet.AddAsync(entity, cancellationToken);
            await _databaseContext.SaveChangesAsync(cancellationToken);
            return entry.Entity;
        }

        /// <inheritdoc/>
        public async Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : EntityBase
        {
            Guard.Against.Null(entity);

            var entitySet = _databaseContext.Set<TEntity>();
            var entry = entitySet.Update(entity);
            await _databaseContext.SaveChangesAsync(cancellationToken);
            return entry.Entity;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync<TEntity>(string id, CancellationToken cancellationToken = default) where TEntity : EntityBase
        {
            var entity = await FindByIdAsync<TEntity>(id, cancellationToken);

            if (entity is not null)
            {
                var entitySet = _databaseContext.Set<TEntity>();
                entitySet.Remove(entity);
                await _databaseContext.SaveChangesAsync(cancellationToken);
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
