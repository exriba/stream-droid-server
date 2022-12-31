namespace StreamDroid.Core.ValueObjects
{
    public sealed class Speech : ValueObject
    {
        public bool Enabled { get; set; }

        public int VoiceIndex { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return VoiceIndex;
            yield return Enabled;
        }
    }
}
