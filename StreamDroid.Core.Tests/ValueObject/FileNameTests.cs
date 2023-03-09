using Ardalis.SmartEnum;
using StreamDroid.Core.Enums;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Tests.ValueObject
{
    public class FileNameTests
    {
        private const string FILE = "file";
        private const string MP4FILE = "file.mp4";
        private const string MP4FILE_COPY = "file_copy.mp4";

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FileName_Throws_InvalidArgs(string name)
        {
            Assert.ThrowsAny<ArgumentException>(() => new FileName(name, MediaExtension.MP3));
        }

        [Fact]
        public void Equal()
        {
            var mediaExtension = MediaExtension.MP4;

            var fileName = new FileName(FILE, mediaExtension);
            var fileName1 = new FileName(FILE, mediaExtension);

            Assert.Equal(fileName, fileName1);
        }

        [Fact]
        public void NotEqual()
        {
            var nameCopy = "file_copy";
            var mediaExtension = MediaExtension.MP4;

            var fileName = new FileName(FILE, mediaExtension);
            var fileName1 = new FileName(nameCopy, mediaExtension);

            Assert.NotEqual(fileName, fileName1);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData(".mp4")]

        public void FileName_FromString_Throws_InvalidArgs(string name)
        {
            Assert.ThrowsAny<ArgumentException>(() => FileName.FromString(name));
        }

        [Theory]
        [InlineData("file.csv")]

        public void FileName_FromString_Throws_InvalidExtension(string name)
        {
            Assert.ThrowsAny<SmartEnumNotFoundException>(() => FileName.FromString(name));
        }

        [Fact]
        public void FileName_FromString_Equal()
        {
            var fileName = FileName.FromString(MP4FILE);
            var fileName1 = FileName.FromString(MP4FILE);

            Assert.Equal(fileName, fileName1);
        }

        [Fact]
        public void FileName_FromString_NotEqual()
        {
            var fileName = FileName.FromString(MP4FILE);
            var fileName1 = FileName.FromString(MP4FILE_COPY);

            Assert.NotEqual(fileName, fileName1);
        }
    }
}
