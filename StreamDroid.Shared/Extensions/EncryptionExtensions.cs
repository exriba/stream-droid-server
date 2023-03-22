using Ardalis.GuardClauses;
using StreamDroid.Shared.Settings;
using System.Security.Cryptography;
using System.Text;

namespace StreamDroid.Shared.Extensions
{
    /// <summary>
    /// Utility class for encryption extensions.
    /// </summary>
    public static class EncryptionExtensions
    {
        private static byte[] _key = Array.Empty<byte>();

        /// <summary>
        /// Initializes encryption properties. 
        /// </summary>
        /// <param name="encryptionSettings">encryption settings</param>
        /// <exception cref="ArgumentNullException">If the encryption settings are null</exception>
        internal static void Configure(EncryptionSettings encryptionSettings)
        {
            Guard.Against.Null(encryptionSettings, nameof(encryptionSettings));
            _key = Encoding.UTF8.GetBytes(encryptionSettings.KeyPhrase);
        }

        /// <summary>
        /// Verifies whether or not a string is in base64.
        /// </summary>
        /// <param name="str">string</param>
        /// <returns><see langword="true"/> if the string is in base64. Otherwise returns <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If the string is null</exception>
        /// <exception cref="ArgumentException">If the string is empty or whitespace string</exception>
        public static bool IsBase64String(this string str)
        {
            Guard.Against.NullOrWhiteSpace(str, nameof(str));
            var buffer = new Span<byte>(new byte[str.Length]);
            return Convert.TryFromBase64String(str, buffer, out _);
        }

        /// <summary>
        /// Encrypts a string using the default key.
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>A base64 encrypted string.</returns>
        /// <exception cref="ArgumentNullException">If the string is null</exception>
        /// <exception cref="ArgumentException">If the string is empty or whitespace string</exception>
        public static string Base64Encrypt(this string str)
        {
            Guard.Against.NullOrWhiteSpace(str, nameof(str));
            return Base64Encrypt(str, _key);
        }

        /// <summary>
        /// Encrypts a string using the given key.
        /// </summary>
        /// <param name="str">string</param>
        /// <param name="keyPhrase">keyphrase</param>
        /// <returns>A base64 encrypted string.</returns>
        /// <exception cref="ArgumentNullException">If the string or keyphrase is null</exception>
        /// <exception cref="ArgumentException">If the string or keyphrase is empty or whitespace string</exception>
        public static string Base64Encrypt(this string str, string keyPhrase)
        {
            Guard.Against.NullOrWhiteSpace(str, nameof(str));
            Guard.Against.NullOrWhiteSpace(keyPhrase, nameof(keyPhrase));
            var key = Encoding.UTF8.GetBytes(keyPhrase);
            return Base64Encrypt(str, key);
        }

        private static string Base64Encrypt(string str, byte[] key)
        {
            using var aesAlg = Aes.Create();
            using var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV);
            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(str);
            }

            var iv = aesAlg.IV;
            var decryptedContent = memoryStream.ToArray();
            var result = new byte[iv.Length + decryptedContent.Length];

            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts an encrypted string using the default key.
        /// </summary>
        /// <param name="str">encrypted string</param>
        /// <returns>A decrypted string.</returns>
        /// <exception cref="ArgumentNullException">If the string is null</exception>
        /// <exception cref="ArgumentException">If the string is empty or whitespace string</exception>
        public static string Base64Decrypt(this string str)
        {
            Guard.Against.NullOrWhiteSpace(str, nameof(str));
            return Base64Decrypt(str, _key);
        }

        /// <summary>
        /// Decrypts an encrypted string using the given key.
        /// </summary>
        /// <param name="str">encrypted string</param>
        /// <param name="keyPhrase">keyphrase</param>
        /// <returns>A decrypted string.</returns>
        /// <exception cref="ArgumentNullException">If the string or keyphrase is null</exception>
        /// <exception cref="ArgumentException">If the string or keyphrase is empty or whitespace string</exception>
        public static string Base64Decrypt(this string str, string keyPhrase)
        {
            Guard.Against.NullOrWhiteSpace(str, nameof(str));
            Guard.Against.NullOrWhiteSpace(keyPhrase, nameof(keyPhrase));
            var key = Encoding.UTF8.GetBytes(keyPhrase);
            return Base64Decrypt(str, key);
        }

        private static string Base64Decrypt(string str, byte[] key)
        {
            var encrypted = Convert.FromBase64String(str);

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
