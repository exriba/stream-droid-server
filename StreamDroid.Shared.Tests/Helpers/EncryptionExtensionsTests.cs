using StreamDroid.Shared.Helpers;
using StreamDroid.Shared.Tests.Common;

namespace StreamDroid.Shared.Tests.Helpers
{
    public class EncryptionExtensionsTests : TestFixture
    {
        private const string TEXT = "EncryptMe";
        private const string SECRET_KEY = "/A?D(G+KbPeShVmY";

        public EncryptionExtensionsTests() : base() { }

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
            var encryptedText = TEXT.Base64Encrypt();
            var isBase64String = encryptedText.IsBase64String();

            Assert.True(isBase64String);
        }

        [Fact]
        public void IsBase64String_False()
        {
            var isBase64String = TEXT.IsBase64String();
            Assert.False(isBase64String);
        }

        [Theory]
        [InlineData("", SECRET_KEY)]
        [InlineData(" ", SECRET_KEY)]
        [InlineData(null, SECRET_KEY)]
        [InlineData(TEXT, "")]
        [InlineData(TEXT, " ")]
        [InlineData(TEXT, null)]
        public void Base64Encrypt_Throws_InvalidArgs(string text, string secretKey)
        {
            Assert.ThrowsAny<ArgumentException>(() => text.Base64Encrypt(keyPhrase: secretKey));
        }

        [Fact]
        public void Base64Encrypt()
        {
            var encryptedText = TEXT.Base64Encrypt(keyPhrase: SECRET_KEY);
            Assert.NotEqual(TEXT, encryptedText);

            encryptedText = TEXT.Base64Encrypt();
            Assert.NotEqual(TEXT, encryptedText);
        }

        [Theory]
        [InlineData("", SECRET_KEY)]
        [InlineData(" ", SECRET_KEY)]
        [InlineData(null, SECRET_KEY)]
        [InlineData(TEXT, "")]
        [InlineData(TEXT, " ")]
        [InlineData(TEXT, null)]
        public void Base64Decrypt_Throws_InvalidArgs(string text, string secretKey)
        {
            Assert.ThrowsAny<ArgumentException>(() => text.Base64Decrypt(keyPhrase: secretKey));
        }

        [Fact]
        public void Base64Decrypt()
        {
            var encryptedText = TEXT.Base64Encrypt(keyPhrase: SECRET_KEY);
            var decryptedText = encryptedText.Base64Decrypt(keyPhrase: SECRET_KEY);
            Assert.Equal(TEXT, decryptedText);

            encryptedText = TEXT.Base64Encrypt();
            decryptedText = encryptedText.Base64Decrypt();
            Assert.Equal(TEXT, decryptedText);
        }
    }
}