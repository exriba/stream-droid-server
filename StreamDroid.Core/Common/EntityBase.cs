using Ardalis.GuardClauses;

namespace StreamDroid.Core.Common
{
    public abstract class EntityBase
    {
        private string _id = string.Empty;
        public string Id 
        {
            get => _id;  
            set 
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(Id));
                _id = value;
            }  
        }
    }
}
