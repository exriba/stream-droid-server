namespace StreamDroid.Core.Exceptions
{
    public sealed class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string id) : base($"Entity {id} not found.")
        {
        }
    }
}
