namespace StreamDroid.Core.Common
{
    /// <summary>
    /// Entity base class.
    /// </summary>
    public abstract class EntityBase
    {
        /// TODO: Consider replacing with custom id class.
        public string Id { get; init; } = string.Empty;

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType()) return false;
            if (ReferenceEquals(this, obj)) return true;
            var that = obj as EntityBase;
            return Id.Equals(that.Id);
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
