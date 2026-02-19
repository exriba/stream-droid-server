using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Core.Interfaces
{
    /// <summary>
    /// Defines generic persistence operations.
    /// </summary>
    /// TODO: 1. Break this into entity specific repositories (Temporary solution).
    /// TODO: 2. Create a specification class to build entity specific queries and consolidate all repositories
    ///          into a generic uber repository that accepts this specification filters.
    public interface IRepository<TEntity> where TEntity : EntityBase
    {
        /// <summary>
        /// Finds an entity by id.
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>An entity.</returns>
        /// <exception cref="ArgumentNullException">If the id is null</exception>
        /// <exception cref="ArgumentException">If the id is empty or whitespace string</exception>
        Task<TEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a collection of entities that matches the given expression.
        /// </summary>
        /// <param name="expression">predicate expression</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>A collection of entities.</returns>
        Task<IReadOnlyCollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves an entity to the database.
        /// </summary>
        /// <param name="entity">entity</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>An entity.</returns>
        /// <exception cref="ArgumentNullException">If the entity is null</exception>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">entity</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>An entity.</returns>
        /// <exception cref="ArgumentNullException">If the entity is null</exception>
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by their id.
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="cancellationToken">cancellation token</param>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
