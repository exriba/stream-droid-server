using StreamDroid.Core.Enums;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Tests.ValueObject
{
    public class FileNameTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FileName_Throws_EmptyName(string name)
        {
            Assert.ThrowsAny<ArgumentException>(() => new FileName(name, Extension.MP3));
            Assert.ThrowsAny<ArgumentException>(() => FileName.FromString(name));
        }

        [Theory]
        [InlineData("file")]
        [InlineData(".mp4")]
        [InlineData("file.csv")]
        public void FileName_Throws_InvalidNames(string name)
        {
            Assert.Throws<ArgumentException>(() => FileName.FromString(name));
        }

        [Fact]
        public void FileName_Created()
        {
            var name = "file.mp4";
            var fileName = new FileName("file", Extension.MP4);
            Assert.Equal(name, fileName.ToString());

            var fileName1 = FileName.FromString(name);
            Assert.True(fileName.Equals(fileName1));
        }
    }
}
