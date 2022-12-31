using Moq;
using Microsoft.AspNetCore.Http;
using StreamDroid.Application.API.Constraints;

namespace StreamDroid.Application.Tests.API.Constraints
{
    public class FileExtensionsAttributeTests
    {
        private readonly FileExtensionsAttribute _fileExtensionsAttribute;

        public FileExtensionsAttributeTests()
        {
            _fileExtensionsAttribute = new FileExtensionsAttribute(new string[] { ".mp3", ".mp4" });
        }

        [Theory]
        [InlineData("file.xml")]
        [InlineData("file.csv")]
        [InlineData("file.json")]
        public void FileExtensionsAttribute_IsValid_Throws_InvalidExtension(string fileName)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(x => x.FileName).Returns(fileName);

            Assert.ThrowsAny<ArgumentException>(() => _fileExtensionsAttribute.IsValid(new IFormFile[] { mockFile.Object }));
        }

        [Theory]
        [InlineData("file.mp3")]
        [InlineData("file.mp4")]
        public void FileExtensionsAttribute_IsValid_True(string fileName)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(x => x.FileName).Returns(fileName);

            var result = _fileExtensionsAttribute.IsValid(new IFormFile[] { mockFile.Object });

            Assert.True(result);
        }
    }
}
