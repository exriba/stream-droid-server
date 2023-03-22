namespace StreamDroid.Core.Exceptions
{
    /// <summary>
    /// Entity not found exception.
    /// </summary>
    public sealed class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string id) : base($"Entity {id} not found.")
        {
        }
    }
}
