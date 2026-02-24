using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Core.Interfaces
{
    public interface IUberRepository
    {
        /// <summary>
        /// Finds an entity by id.
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>An entity.</returns>
        /// <exception cref="ArgumentNullException">If the id is null</exception>
        /// <exception cref="ArgumentException">If the id is empty or whitespace string</exception>
        Task<TEntity?> FindByIdAsync<TEntity>(string id, CancellationToken cancellationToken = default) where TEntity : EntityBase;

        /// <summary>
        /// Counts the number of entities that satisfy the given expression.
        /// </summary>
        /// <param name="expression">predicate expression</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>The number of entities..</returns>
        Task<long> CountAsync<TEntity>(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellationToken = default) where TEntity : EntityBase;

        /// <summary>
        /// Retrieves an enumeration of entities that matches the given expression, offset and limit.
        /// </summary>
        /// <param name="expression">predicate expression</param>
        /// <param name="offset">offset</param>
        /// <param name="limit">limit</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Asynchronous enumeration of entities.</returns>
        IAsyncEnumerable<TEntity> FindStreamAsync<TEntity>(Expression<Func<TEntity, bool>>? expression = null, int? offset = null, int? limit = null, CancellationToken cancellationToken = default) where TEntity : EntityBase;

        /// <summary>
        /// Retrieves a collection of entities that matches the given expression, offset and limit.
        /// </summary>
        /// <param name="expression">predicate expression</param>
        /// <param name="offset">offset</param>
        /// <param name="limit">limit</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>A collection of entities.</returns>
        Task<IReadOnlyCollection<TEntity>> FindListAsync<TEntity>(Expression<Func<TEntity, bool>>? expression = null, int? offset = null, int? limit = null, CancellationToken cancellationToken = default) where TEntity : EntityBase;

        /// <summary>
        /// Saves an entity to the database.
        /// </summary>
        /// <param name="entity">entity</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>An entity.</returns>
        /// <exception cref="ArgumentNullException">If the entity is null</exception>
        Task<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : EntityBase;

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">entity</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>An entity.</returns>
        /// <exception cref="ArgumentNullException">If the entity is null</exception>
        Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : EntityBase;

        /// <summary>
        /// Deletes an entity by their id.
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="cancellationToken">cancellation token</param>
        Task DeleteAsync<TEntity>(string id, CancellationToken cancellationToken = default) where TEntity : EntityBase;
    }
}
