using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    public interface IUberRepository : IDisposable
    {
        Task<IReadOnlyCollection<T>> FindAll<T>() where T : EntityBase;
        Task<IReadOnlyCollection<T>> Find<T>(Expression<Func<T, bool>> expression) where T : EntityBase;
        Task<T> Save<T>(T entity) where T : EntityBase;
        Task Delete<T>(T entity) where T : EntityBase;
    }
}
