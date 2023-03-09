namespace StreamDroid.Core.ValueObjects
{
    public class Preferences : ValueObject
    {
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
