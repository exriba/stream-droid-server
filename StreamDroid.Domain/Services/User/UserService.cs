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
        private readonly IRepository<Entities.User> _repository;

        public UserService(IAuthApi authApi, 
                           IRepository<Entities.User> repository)
        {
            _authApi = authApi;
            _repository = repository;
        }

        public async Task<UserDto?> FindUserByIdAsync(string userId)
        {
            var user = await FetchUserByIdAsync(userId);
            return user is not null ? UserDto.FromEntity(user) : null;
        }

        public async Task<UserDto> AuthenticateUserAsync(string code)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));

            var token = await _authApi.GetAccessTokenFromCodeAsync(code, CancellationToken.None);
            var userData = await _authApi.ValidateAccessTokenAsync(token.AccessToken, CancellationToken.None);
            var user = await FetchUserByIdAsync(userData.UserId);

            if (user is null)
            {
                user = new Entities.User
                {
                    Id = userData.UserId,
                    Name = userData.Login,
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken,
                };
                user = await _repository.AddAsync(user);
                return UserDto.FromEntity(user);
            }

            user.Name = userData.Login;
            user.AccessToken = token.AccessToken;
            user.RefreshToken = token.RefreshToken;
            user = await _repository.UpdateAsync(user);
            return UserDto.FromEntity(user);
        }

        public async Task<TokenRefreshPolicy> CreateTokenRefreshPolicyAsync(string userId)
        {
            var user = await FetchUserAsync(userId);

            async Task<string> refreshToken(string userId)
            {
                var refreshToken = user.RefreshToken.Base64Decrypt();
                var token = await _authApi.RefreshAccessTokenAsync(refreshToken, CancellationToken.None);
                user.AccessToken = token.AccessToken;
                user.RefreshToken = token.RefreshToken;
                user = await _repository.UpdateAsync(user);
                return token.AccessToken;
            };

            var accessToken = user.AccessToken.Base64Decrypt();
            return new TokenRefreshPolicy(userId, accessToken, refreshToken);
        }

        public async Task<Preferences> UpdateUserPreferencesAsync(string userId, Preferences preferences)
        {
            Guard.Against.Null(preferences, nameof(preferences));

            var user = await FetchUserAsync(userId);
            user.Preferences = preferences;
            user = await _repository.UpdateAsync(user);
            return user.Preferences;
        }

        #region Helpers
        private async Task<Entities.User> FetchUserAsync(string userId)
        {
            return await FetchUserByIdAsync(userId) ?? throw new EntityNotFoundException(userId);
        }

        private async Task<Entities.User?> FetchUserByIdAsync(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

            return await _repository.FindByIdAsync(userId);
        }
        #endregion
    }
}
