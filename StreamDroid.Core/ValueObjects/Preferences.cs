namespace StreamDroid.Core.ValueObjects
{
    /// <summary>
    /// Preferences value object.
    /// </summary>
    public class Preferences : ValueObject
    {
        /// <summary>
        /// Default volume
        /// </summary>
        public int DefaultVolume { get; private init; }

        public Preferences(int defaultVolume = 100) 
        {
            DefaultVolume = defaultVolume;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return DefaultVolume;
        }
    }
}
