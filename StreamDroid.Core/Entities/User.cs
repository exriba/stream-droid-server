using Ardalis.GuardClauses;
using StreamDroid.Core.Common;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Shared.Extensions;

namespace StreamDroid.Core.Entities
{
    public partial class User : EntityBase
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(Name));
                _name = value;
            }
        }

        private string _userKey = string.Empty;
        public string UserKey
        {
            get => _userKey;
            set
            {
                _userKey = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString() : value;
            }
        }

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

        private Preferences _preferences = new Preferences();
        public Preferences Preferences
        {
            get => _preferences;
            set
            {
                _preferences = value;
            }
        }
    }
}
