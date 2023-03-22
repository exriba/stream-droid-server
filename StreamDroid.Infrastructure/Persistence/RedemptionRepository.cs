using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using StreamDroid.Core.Entities;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    /// <summary>
    /// Default implementation of <see cref="IRedemptionRepository"/>.
    /// </summary>
    internal class RedemptionRepository : IRedemptionRepository
    {
        private readonly DbSet<Redemption> _entitySet;
        private readonly DatabaseContext _databaseContext;

        public RedemptionRepository(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
            _entitySet = databaseContext.Set<Redemption>();
        }

        /// <inheritdoc/>
        public async Task<Redemption> AddAsync(Redemption redemption)
        {
            Guard.Against.Null(redemption);

            var e = await _entitySet.AddAsync(redemption);
            await _databaseContext.SaveChangesAsync();
            return e.Entity;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<Redemption>> FindAsync(Expression<Func<Redemption, bool>>? expression = null)
        {
            return expression is null
                ? await _entitySet.Include(x => x.Reward).AsNoTracking().ToListAsync()
                : await _entitySet.Include(x => x.Reward).Where(expression).AsNoTracking().ToListAsync();
        }

        public void Dispose()
        {
            _databaseContext.Dispose();
        }
    }
}
