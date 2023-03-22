namespace StreamDroid.Core.ValueObjects
{
    /// <summary>
    /// Speech value object.
    /// </summary>
    public sealed class Speech : ValueObject
    {
        /// <summary>
        /// Enabled
        /// </summary>
        public bool Enabled { get; private init; } 

        /// <summary>
        /// Voice index
        /// </summary>
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
