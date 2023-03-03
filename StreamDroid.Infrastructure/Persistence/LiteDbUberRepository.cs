using LiteDB;
using Ardalis.GuardClauses;
using StreamDroid.Core.Common;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using StreamDroid.Infrastructure.Settings;

namespace StreamDroid.Infrastructure.Persistence
{
    public class LiteDbUberRepository : IUberRepository
    {
        private readonly ILiteDatabase _database;

        public LiteDbUberRepository(IOptions<LiteDbSettings> options)
        {
            Guard.Against.NullOrWhiteSpace(options.Value.ConnectionString, nameof(options.Value.ConnectionString));

            _database = new LiteDatabase(options.Value.ConnectionString);
        }

        public Task<IReadOnlyCollection<T>> FindAll<T>() where T : EntityBase
        {
            var collection = _database.GetCollection<T>();
            var entities = collection.FindAll().ToList();
            return Task.FromResult<IReadOnlyCollection<T>>(entities);
        }

        public Task<IReadOnlyCollection<T>> Find<T>(Expression<Func<T, bool>> expression) where T : EntityBase
        {
            Guard.Against.Null(expression);

            var collection = _database.GetCollection<T>();
            var entities = collection.FindAll().ToList();
            return Task.FromResult<IReadOnlyCollection<T>>(entities);
        }

        public Task<T> Save<T>(T entity) where T : EntityBase
        {
            Guard.Against.Null(entity);

            var collection = _database.GetCollection<T>();
            collection.Upsert(entity);
            return Task.FromResult(entity);
        }

        public Task Delete<T>(T entity) where T : EntityBase
        {
            Guard.Against.Null(entity);

            var collection = _database.GetCollection<T>();
            collection.Delete(entity.Id);
            return Task.CompletedTask;
        }

        public virtual void Dispose() => _database?.Dispose();
    }
}
