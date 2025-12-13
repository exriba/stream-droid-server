using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using StreamDroid.Core.Entities;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    /// <summary>
    /// Default implementation of <see cref="IRedemptionRepository"/>.
    /// </summary>
    internal class RedemptionRepository : IRedemptionRepository, IDisposable, IAsyncDisposable
    {
        private readonly DbSet<Redemption> _entitySet;
        private readonly DatabaseContext _databaseContext;

        public RedemptionRepository(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
            _entitySet = databaseContext.Set<Redemption>();
        }

        /// <inheritdoc/>
        public async Task<Redemption> AddAsync(Redemption redemption, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(redemption);

            var e = await _entitySet.AddAsync(redemption, cancellationToken);
            await _databaseContext.SaveChangesAsync(cancellationToken);
            return e.Entity;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<Redemption>> FindAsync(Expression<Func<Redemption, bool>>? expression = null, CancellationToken cancellationToken = default)
        {
            return expression is null
                ? await _entitySet.Include(x => x.Reward).AsNoTracking().ToListAsync(cancellationToken)
                : await _entitySet.Include(x => x.Reward).Where(expression).AsNoTracking().ToListAsync(cancellationToken);
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
