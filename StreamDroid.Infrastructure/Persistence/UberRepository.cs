using LiteDB;
using Ardalis.GuardClauses;
using StreamDroid.Core.Common;
using StreamDroid.Infrastructure.Settings;
using System.Linq.Expressions;

namespace StreamDroid.Infrastructure.Persistence
{
    // Use tasks and make all these methods async
    public class UberRepository : IUberRepository
    {
        private readonly ILiteDatabase _database;

        public UberRepository(IPersistenceSettings settings)
        {
            Guard.Against.NullOrWhiteSpace(settings.ConnectionString, nameof(settings.ConnectionString));
            _database = new LiteDatabase(settings.ConnectionString);
        }

        public virtual IReadOnlyCollection<T> FindAll<T>() where T : EntityBase
        {
            var collection = _database.GetCollection<T>();
            return collection.FindAll().ToList();
        }

        public virtual IReadOnlyCollection<T> Find<T>(Expression<Func<T, bool>> expression) where T : EntityBase
        {
            Guard.Against.Null(expression);
            var collection = _database.GetCollection<T>();
            return collection.Find(expression).ToList();
        }

        public virtual T Save<T>(T entity) where T : EntityBase
        {
            Guard.Against.Null(entity);
            var collection = _database.GetCollection<T>();
            collection.Upsert(entity);
            return entity;
        }

        public virtual void Delete<T>(T entity) where T : EntityBase
        {
            Guard.Against.Null(entity);
            var collection = _database.GetCollection<T>();
            collection.Delete(entity.Id);
        }

        public virtual void Dispose() => _database?.Dispose();
    }
}
