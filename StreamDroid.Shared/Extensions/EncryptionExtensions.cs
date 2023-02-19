using Ardalis.GuardClauses;
using StreamDroid.Shared.Settings;
using System.Security.Cryptography;
using System.Text;

namespace StreamDroid.Shared.Extensions
{
    public static class EncryptionExtensions
    {
        private static byte[] _key = Array.Empty<byte>();

        internal static void Configure(EncryptionSettings encryptionSettings)
        {
            Guard.Against.Null(encryptionSettings, nameof(encryptionSettings));
            _key = Encoding.UTF8.GetBytes(encryptionSettings.KeyPhrase);
        }

        public static bool IsBase64String(this string text)
        {
            Guard.Against.NullOrWhiteSpace(text, nameof(text));
            var buffer = new Span<byte>(new byte[text.Length]);
            return Convert.TryFromBase64String(text, buffer, out _);
        }

        public static string Base64Encrypt(this string text)
        {
            Guard.Against.NullOrWhiteSpace(text, nameof(text));
            return Base64Encrypt(text, _key);
        }

        public static string Base64Encrypt(this string text, string keyPhrase)
        {
            Guard.Against.NullOrWhiteSpace(text, nameof(text));
            Guard.Against.NullOrWhiteSpace(keyPhrase, nameof(keyPhrase));
            var key = Encoding.UTF8.GetBytes(keyPhrase);
            return Base64Encrypt(text, key);
        }

        private static string Base64Encrypt(string text, byte[] key)
        {
            using var aesAlg = Aes.Create();
            using var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV);
            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(text);
            }

            var iv = aesAlg.IV;
            var decryptedContent = memoryStream.ToArray();
            var result = new byte[iv.Length + decryptedContent.Length];

            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

            return Convert.ToBase64String(result);
        }

        public static string Base64Decrypt(this string cipherText)
        {
            Guard.Against.NullOrWhiteSpace(cipherText, nameof(cipherText));
            return Base64Decrypt(cipherText, _key);
        }

        public static string Base64Decrypt(this string cipherText, string keyPhrase)
        {
            Guard.Against.NullOrWhiteSpace(cipherText, nameof(cipherText));
            Guard.Against.NullOrWhiteSpace(keyPhrase, nameof(keyPhrase));
            var key = Encoding.UTF8.GetBytes(keyPhrase);
            return Base64Decrypt(cipherText, key);
        }

        private static string Base64Decrypt(string cipherText, byte[] key)
        {
            var encrypted = Convert.FromBase64String(cipherText);

            var iv = new byte[16];
            var cipher = new byte[encrypted.Length - iv.Length];

            Buffer.BlockCopy(encrypted, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(encrypted, iv.Length, cipher, 0, cipher.Length);

            string? decrypted = null;
            using var aesAlg = Aes.Create();
            using var decryptor = aesAlg.CreateDecryptor(key, iv);
            using (var memoryStream = new MemoryStream(cipher))
            {
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);
                decrypted = streamReader.ReadToEnd();
            }

            return decrypted;
        }
    }
}
