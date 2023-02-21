using Ardalis.GuardClauses;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;
using StreamDroid.Domain.RefreshPolicy;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Shared.Extensions;
using StreamDroid.Core.Exceptions;
using SharpTwitch.Auth;
using StreamDroid.Domain.DTOs;

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

        public UserDto? FindById(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

            var users = _uberRepository.Find<Entities.User>(u => u.Id.Equals(userId));
            return users.Any() ? UserDto.FromEntity(users.First()) : null;
        }

        public async Task<UserDto> Authenticate(string code)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));

            var token = await _authApi.GetAccessTokenFromCodeAsync(code, CancellationToken.None);
            var userData = await _authApi.ValidateAccessTokenAsync(token.AccessToken, CancellationToken.None);

            var user = new Entities.User
            {
                Id = userData.UserId,
                Name = userData.Login,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
            };
            user = _uberRepository.Save(user);

            return UserDto.FromEntity(user);
        }

        public TokenRefreshPolicy CreateTokenRefreshPolicy(string userId)
        {
            var user = FindUser(userId);

            async Task<string> refreshToken(string userId)
            {
                var refreshToken = user.RefreshToken.Base64Decrypt();
                var token = await _authApi.RefreshAccessTokenAsync(refreshToken, CancellationToken.None);
                user.AccessToken = token.AccessToken;
                user.RefreshToken = token.RefreshToken;
                user = _uberRepository.Save(user);
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
            user = _uberRepository.Save(user);
            return user.Preferences;
        }

        private Entities.User FindUser(string userId)
        {
            return FindUserById(userId) ?? throw new EntityNotFoundException(userId);
        }

        private Entities.User? FindUserById(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
            var users = _uberRepository.Find<Entities.User>(u => u.Id.Equals(userId));
            return users.FirstOrDefault();
        }
    }
}
