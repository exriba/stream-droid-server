using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Tests.ValueObject
{
    public class SpeechTests
    {
        [Fact]
        public void Speech_Equal()
        {
            var speech = new Speech
            {
                VoiceIndex = 1,
                Enabled = true,
            };

            var speech1 = new Speech
            {
                VoiceIndex = 1,
                Enabled = true,
            };

            Assert.True(speech.Equals(speech1));
        }

        [Fact]
        public void Speech_NotEqual()
        {
            var speech = new Speech
            {
                VoiceIndex = 1,
                Enabled = true,
            };

            var speech1 = new Speech
            {
                VoiceIndex = 1,
                Enabled = false,
            };
            
            Assert.False(speech.Equals(speech1));
        }
    }
}
