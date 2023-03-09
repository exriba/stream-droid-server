using Ardalis.SmartEnum;
using StreamDroid.Core.Enums;

namespace StreamDroid.Core.Tests.Enums
{
    public class MediaExtensionTests
    {
        private const string MP3 = "MP3";
        private const string MP4 = "MP4";
        private const string UNKNOWN = "UNKNOWN";

        [Theory]
        [InlineData(-1)]
        public void MediaExtension_FromValue_Throws_InvalidArgs(int value)
        {
            Assert.ThrowsAny<SmartEnumNotFoundException>(() => MediaExtension.FromValue(value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void MediaExtension_FromValue(int value)
        {
            var mediaExtension = MediaExtension.FromValue(value);

            Assert.NotNull(mediaExtension);
        }

        [Theory]
        [InlineData(UNKNOWN)]
        public void MediaExtension_FromName_Throws_InvalidArgs(string name)
        {
            Assert.ThrowsAny<SmartEnumNotFoundException>(() => MediaExtension.FromName(name));
        }

        [Theory]
        [InlineData(MP3)]
        [InlineData(MP4)]
        public void MediaExtension_FromName(string name)
        {
            var mediaExtension = MediaExtension.FromName(name);

            Assert.NotNull(mediaExtension);
        }
    }
}