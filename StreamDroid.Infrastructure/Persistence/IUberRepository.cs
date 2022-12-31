using StreamDroid.Core.Common;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    public interface IUberRepository : IDisposable
    {
        IReadOnlyCollection<T> FindAll<T>() where T : EntityBase;
        IReadOnlyCollection<T> Find<T>(Expression<Func<T, bool>> expression) where T : EntityBase;
        T Save<T>(T entity) where T : EntityBase;
        void Delete<T>(T entity) where T : EntityBase;
    }
}
