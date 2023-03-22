using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Exceptions
{
    /// <summary>
    /// Duplicate asset exception. 
    /// </summary>
    public class DuplicateAssetException : Exception
    {
        public DuplicateAssetException(FileName fileName) : base($"The collection already contains asset {fileName}.")
        {
        }
    }
}
