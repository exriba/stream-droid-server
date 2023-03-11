using StreamDroid.Core.Entities;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    public interface IRedemptionRepository : IDisposable
    {
        Task<Redemption> AddAsync(Redemption redemption);
        Task<IReadOnlyCollection<Redemption>> FindAsync(Expression<Func<Redemption, bool>>? expression = null);
    }
}
