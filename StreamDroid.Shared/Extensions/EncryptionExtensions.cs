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
        private const int KEY_SIZE_BYTES = 32;
        private const int PBKDF2_ITERATION = 300000;

        private static byte[] _masterKey = Array.Empty<byte>();

        /// <summary>
        /// Initializes encryption properties. 
        /// </summary>
        /// <param name="encryptionSettings">encryption settings</param>
        /// <exception cref="ArgumentNullException">If the encryption settings are null</exception>
        /// <exception cref="InvalidOperationException">If the master key has already been configured</exception>
        internal static void Configure(EncryptionSettings encryptionSettings)
        {
            Guard.Against.Null(encryptionSettings, nameof(encryptionSettings));

            if (encryptionSettings.Salt.Length < 16)
                throw new ArgumentException("EcryptionSettings - Salt must be 16+ characters.");

            if (_masterKey.Length != 0)
                throw new InvalidOperationException("Encryption already configured.");

            _masterKey = DeriveKeyFromPassphrase(encryptionSettings.KeyPhrase, encryptionSettings.Salt);
        }

        private static byte[] DeriveKeyFromPassphrase(string passphrase, string salt)
        {
            var saltBytes = Encoding.UTF8.GetBytes(salt);
            using var dkf = new Rfc2898DeriveBytes(
                passphrase,
                saltBytes,
                PBKDF2_ITERATION,
                HashAlgorithmName.SHA256
            );
            return dkf.GetBytes(KEY_SIZE_BYTES);
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
        /// Encrypts a string using the master key.
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>A base64 encrypted string.</returns>
        /// <exception cref="ArgumentNullException">If the string is null</exception>
        /// <exception cref="ArgumentException">If the string is empty or whitespace string</exception>
        public static string Base64Encrypt(this string str)
        {
            ValidateArguments(str);

            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(str);
            byte[] cipherText = new byte[plaintextBytes.Length];
            byte[] tag = new byte[16];

            using var aes = new AesGcm(_masterKey, 16);
            aes.Encrypt(nonce, plaintextBytes, cipherText, tag);

            byte[] result = new byte[nonce.Length + tag.Length + cipherText.Length];

            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherText, 0, result, nonce.Length + tag.Length, cipherText.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts an encrypted string using the master key.
        /// </summary>
        /// <param name="str">encrypted string</param>
        /// <returns>A decrypted string.</returns>
        /// <exception cref="ArgumentNullException">If the string is null</exception>
        /// <exception cref="ArgumentException">If the string is empty or whitespace string</exception>
        public static string Base64Decrypt(this string str)
        {
            ValidateArguments(str);

            byte[] fullCipher = Convert.FromBase64String(str);

            if (fullCipher.Length < 28)
                throw new ArgumentException("Invalid encrypted payload.");

            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] cipherText = new byte[fullCipher.Length - 28];

            Buffer.BlockCopy(fullCipher, 0, nonce, 0, 12);
            Buffer.BlockCopy(fullCipher, 12, tag, 0, 16);
            Buffer.BlockCopy(fullCipher, 28, cipherText, 0, cipherText.Length);

            byte[] plaintextBytes = new byte[cipherText.Length];

            using var aes = new AesGcm(_masterKey, 16);
            aes.Decrypt(nonce, cipherText, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        /// <summary>
        /// Validates arguments
        /// </summary>
        /// <param name="str"></param>
        /// <exception cref="ArgumentNullException">If the string is null</exception>
        /// <exception cref="ArgumentException">If the string is empty or whitespace string</exception>
        /// <exception cref="InvalidOperationException">If the master key is not configured</exception>
        private static void ValidateArguments(string str)
        {
            Guard.Against.NullOrWhiteSpace(str, nameof(str));

            if (_masterKey.Length == 0)
                throw new InvalidOperationException("Encryption not configured.");
        }
    }
}
