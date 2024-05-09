using StreamDroid.Core.Entities;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    /// <summary>
    /// Defines Redemption persistence operations.
    /// </summary>
    public interface IRedemptionRepository
    {
        /// <summary>
        /// Saves a redemption to the database.
        /// </summary>
        /// <param name="redemption">redemption</param>
        /// <returns>A redemption entity.</returns>
        /// <exception cref="ArgumentNullException">If the redemption is null.</exception>
        Task<Redemption> AddAsync(Redemption redemption);

        /// <summary>
        /// Retrieves a collection of redemptions that matches the given expression.
        /// </summary>
        /// <param name="expression">predicate expression</param>
        /// <returns>A collection of redemptions</returns>
        Task<IReadOnlyCollection<Redemption>> FindAsync(Expression<Func<Redemption, bool>>? expression = null);
    }
}
