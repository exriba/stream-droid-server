using StreamDroid.Shared.Extensions;
using StreamDroid.Shared.Tests.Common;

namespace StreamDroid.Shared.Tests.Extensions
{
    public class EncryptionExtensionsTests : IClassFixture<TestFixture>
    {
        private const string TEXT = "EncryptMe";

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void IsBase64String_Throws_InvalidArgs(string text)
        {
            Assert.ThrowsAny<ArgumentException>(() => text.IsBase64String());
        }

        [Fact]
        public void IsBase64String_True()
        {
            var encryptedTextWithDefaultKeyPhrase = TEXT.Base64Encrypt();
            var isBase64String = encryptedTextWithDefaultKeyPhrase.IsBase64String();

            Assert.True(isBase64String);
        }

        [Fact]
        public void IsBase64String_False()
        {
            var isBase64String = TEXT.IsBase64String();

            Assert.False(isBase64String);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Base64Encrypt_Throws_InvalidArgs(string? text)
        {
            Assert.ThrowsAny<ArgumentException>(() => text.Base64Encrypt());
        }

        [Fact]
        public void Base64Encrypt()
        {
            var encryptedText = TEXT.Base64Encrypt();

            Assert.NotEqual(TEXT, encryptedText);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Base64Decrypt_Throws_InvalidArgs(string? text)
        {
            Assert.ThrowsAny<ArgumentException>(() => text.Base64Decrypt());
        }

        [Fact]
        public void Base64Decrypt()
        {
            var encryptedText = TEXT.Base64Encrypt();
            var decryptedText = encryptedText.Base64Decrypt();

            Assert.Equal(TEXT, decryptedText);
        }
    }
}