namespace StreamDroid.Core.ValueObjects
{
    public sealed class Speech : ValueObject
    {
        public bool Enabled { get; private init; } 

        public int VoiceIndex { get; private init; }

        public Speech(bool enabled = false, int voiceIndex = 0) 
        {
            Enabled = enabled;
            VoiceIndex = voiceIndex;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return VoiceIndex;
            yield return Enabled;
        }
    }
}
