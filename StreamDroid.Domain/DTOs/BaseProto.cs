using Mapster;

namespace StreamDroid.Domain.DTOs
{
    public abstract class BaseProto<TProto, TEntity> : IRegister
        where TProto : class, new()
        where TEntity : class, new()
    {
        private TypeAdapterConfig Config { get; set; }

        public static TProto FromEntity(TEntity entity)
        {
            return entity.Adapt<TProto>();
        }

        public virtual void AddCustomMappings() { }

        protected TypeAdapterSetter<TEntity, TProto> SetCustomMappings() => Config.ForType<TEntity, TProto>();

        public void Register(TypeAdapterConfig config)
        {
            Config = config;
            AddCustomMappings();
        }
    }
}
