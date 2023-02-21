using Ardalis.GuardClauses;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;
using StreamDroid.Domain.RefreshPolicy;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Shared.Extensions;
using StreamDroid.Core.Exceptions;
using SharpTwitch.Auth;

namespace StreamDroid.Domain.Services.User
{
    public sealed class UserService : IUserService
    {
        private readonly IAuthApi _authApi;
        private readonly IUberRepository _uberRepository;

        public UserService(IAuthApi authApi, IUberRepository uberRepository)
        {
            _authApi = authApi;
            _uberRepository = uberRepository;
        }

        public Entities.User? FindById(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

            var users = _uberRepository.Find<Entities.User>(u => u.Id.Equals(userId));
            return users.FirstOrDefault();
        }

        public async Task<Entities.User> Authenticate(string code)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));

            var token = await _authApi.GetAccessTokenFromCodeAsync(code, CancellationToken.None);
            var userData = await _authApi.ValidateAccessTokenAsync(token.AccessToken, CancellationToken.None);
            return Save(userData.UserId, userData.Login, token.AccessToken, token.RefreshToken);
        }

        public TokenRefreshPolicy CreateTokenRefreshPolicy(string userId)
        {
            var user = FindUser(userId);

            async Task<string> refreshToken(string userId)
            {
                var refreshToken = user.RefreshToken.Base64Decrypt();
                var token = await _authApi.RefreshAccessTokenAsync(refreshToken, CancellationToken.None);
                Save(user.Id, user.Name, token.AccessToken, token.RefreshToken);
                return token.AccessToken;
            };

            var accessToken = user.AccessToken.Base64Decrypt();
            return new TokenRefreshPolicy(userId, accessToken, refreshToken);
        }

        public Preferences UpdatePreferences(string userId, Preferences preferences)
        {
            Guard.Against.Null(preferences, nameof(preferences));

            var user = FindUser(userId);
            user.Preferences = preferences;
            _uberRepository.Save(user);
            return user.Preferences;
        }

        private Entities.User FindUser(string userId)
        {
            return FindById(userId) ?? throw new EntityNotFoundException(userId);
        }

        private Entities.User Save(string id, string name, string accessToken, string refreshToken)
        {
            var user = FindById(id) ?? new Entities.User
            {
                Id = id,
                Name = name,
            };
            user.AccessToken = accessToken;
            user.RefreshToken = refreshToken;
            return _uberRepository.Save(user);
        }
    }
}
