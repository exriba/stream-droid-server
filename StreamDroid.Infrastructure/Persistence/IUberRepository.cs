using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    public interface IUberRepository : IDisposable
    {
        Task<IReadOnlyCollection<T>> FindAllAsync<T>() where T : EntityBase;
        Task<IReadOnlyCollection<T>> FindAsync<T>(Expression<Func<T, bool>> expression) where T : EntityBase;
        Task<T> AddAsync<T>(T entity) where T : EntityBase;
        Task<T> UpdateAsync<T>(T entity) where T : EntityBase;
        Task DeleteAsync<T>(T entity) where T : EntityBase;
    }
}
