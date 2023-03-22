using Ardalis.GuardClauses;
using StreamDroid.Core.Common;
using StreamDroid.Core.Enums;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Shared.Extensions;

namespace StreamDroid.Core.Entities
{
    /// <summary>
    /// An entity that contains user details. 
    /// </summary>
    public class User : EntityBase
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Preferences
        /// </summary>
        public Preferences Preferences { get; set; } = new Preferences();

        /// <summary>
        /// User type
        /// </summary>
        public UserType UserType { get; set; } = UserType.NORMAL;

        /// <summary>
        /// User streaming key
        /// </summary>
        public Guid UserKey { get; private init; } = Guid.NewGuid();

        private string _accessToken = string.Empty;
        /// <summary>
        /// Encrypted access token
        /// </summary>
        public string AccessToken
        {
            get => _accessToken;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(AccessToken));
                _accessToken = value.IsBase64String() ? value : value.Base64Encrypt();
            }
        }

        private string _refreshToken = string.Empty;
        /// <summary>
        /// Encrypted access token
        /// </summary>
        public string RefreshToken
        {
            get => _refreshToken;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(RefreshToken));
                _refreshToken = value.IsBase64String() ? value : value.Base64Encrypt();
            }
        }
    }
}
