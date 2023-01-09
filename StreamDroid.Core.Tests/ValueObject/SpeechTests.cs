using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Tests.ValueObject
{
    public class SpeechTests
    {
        [Fact]
        public void Equal()
        {
            var speech = new Speech(true, 1);
            var speech1 = new Speech(true, 1);

            Assert.Equal(speech, speech1);
        }

        [Fact]
        public void NotEqual()
        {
            var speech = new Speech(true, 1);
            var speech1 = new Speech(false, 1);

            Assert.NotEqual(speech, speech1);
        }
    }
}
