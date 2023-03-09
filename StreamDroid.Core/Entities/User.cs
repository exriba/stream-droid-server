using Ardalis.GuardClauses;
using StreamDroid.Core.Common;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Shared.Extensions;

namespace StreamDroid.Core.Entities
{
    public partial class User : EntityBase
    {
        public string Name { get; set; } = string.Empty;

        public Preferences Preferences { get; set; } = new Preferences();

        public Guid UserKey { get; private init; } = Guid.NewGuid();

        private string _accessToken = string.Empty;
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
