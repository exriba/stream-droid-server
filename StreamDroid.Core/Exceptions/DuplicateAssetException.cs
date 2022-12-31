using StreamDroid.Core.Entities;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Exceptions
{
    public class DuplicateAssetException : Exception
    {
        public DuplicateAssetException(Reward reward, FileName fileName) : base($"Reward {reward.Title} contains asset {fileName}.")
        {
        }
    }
}
