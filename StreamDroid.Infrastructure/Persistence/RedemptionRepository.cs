using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using StreamDroid.Core.Entities;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    internal class RedemptionRepository : IRedemptionRepository
    {
        private readonly DbSet<Redemption> _entitySet;
        private readonly DatabaseContext _databaseContext;

        public RedemptionRepository(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
            _entitySet = databaseContext.Set<Redemption>();
        }

        public async Task<Redemption> AddAsync(Redemption redemption)
        {
            Guard.Against.Null(redemption);

            var e = await _entitySet.AddAsync(redemption);
            await _databaseContext.SaveChangesAsync();
            return e.Entity;
        }

        public async Task<IReadOnlyCollection<Redemption>> FindAsync(Expression<Func<Redemption, bool>>? expression = null)
        {
            return expression is null
                ? await _entitySet.Include(x => x.Reward).ToListAsync()
                : await _entitySet.Include(x => x.Reward).Where(expression).ToListAsync();
        }

        public void Dispose()
        {
            _databaseContext.Dispose();
        }
    }
}
