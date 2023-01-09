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
        [InlineData("file")]
        [InlineData(".mp4")]
        [InlineData("file.csv")]
        public void FileName_Throws_InvalidArgs(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                Assert.ThrowsAny<ArgumentException>(() => new FileName(name, Extension.MP3));
            Assert.ThrowsAny<ArgumentException>(() => FileName.FromString(name));
        }

        [Fact]
        public void FileName_FromString_Equal()
        {
            var name = "file.mp4";

            var fileName = FileName.FromString(name);
            var fileName1 = FileName.FromString(name);

            Assert.Equal(fileName, fileName1);
        }

        [Fact]
        public void FileName_FromString_NotEqual()
        {
            var name = "file.mp4";
            var name1 = "file1.mp4";

            var fileName = FileName.FromString(name);
            var fileName1 = FileName.FromString(name1);

            Assert.NotEqual(fileName, fileName1);
        }

        [Fact]
        public void Equal()
        {
            var name = "file";
            var extension = Extension.MP4;

            var fileName = new FileName(name, extension);
            var fileName1 = new FileName(name, extension);

            Assert.Equal(fileName, fileName1);
        }

        [Fact]
        public void NotEqual()
        {
            var name = "file";
            var name1 = "file2";
            var extension = Extension.MP4;

            var fileName = new FileName(name, extension);
            var fileName1 = new FileName(name1, extension);

            Assert.NotEqual(fileName, fileName1);
        }
    }
}
