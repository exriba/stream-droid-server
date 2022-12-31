using Ardalis.GuardClauses;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;
using SharpTwitch.Core.Interfaces;
using SharpTwitch.Auth;
using StreamDroid.Shared.Helpers;
using StreamDroid.Domain.Models;

namespace StreamDroid.Domain.User
{
    public sealed class UserService : IUserService
    {
        private readonly AuthApi _authApi;
        private readonly IUberRepository _uberRepository;

        public UserService(IApiCore apiCore, ICoreSettings coreSettings, IUberRepository uberRepository)
        {
            _uberRepository = uberRepository;
            _authApi = new AuthApi(coreSettings, apiCore);
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
            var user = FindById(userId);
            async Task<string> refreshToken(Entities.User user) => await RefreshAccessToken(user);
            return new TokenRefreshPolicy(user, refreshToken);
        }

        public async Task<string> RefreshAccessToken(Entities.User user)
        {
            var refreshToken = user.RefreshToken.Base64Decrypt();
            var token = await _authApi.RefreshAccessTokenAsync(refreshToken, CancellationToken.None);
            Save(user.Id, user.Name, token.AccessToken, token.RefreshToken);
            return token.AccessToken;
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
