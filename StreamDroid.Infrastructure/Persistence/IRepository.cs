using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    public interface IRepository<TEntity> where TEntity : EntityBase
    {
        Task<TEntity?> FindByIdAsync(string id);
        Task<IReadOnlyCollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>>? expression = null);
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task DeleteAsync(string id);
    }
}
