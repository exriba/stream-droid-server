namespace StreamDroid.Core.Common
{
    /// <summary>
    /// Entity base class.
    /// </summary>
    public abstract class EntityBase
    {
        /// TODO: Consider replacing with custom id class.
        public string Id { get; init; } = string.Empty;
    }
}
