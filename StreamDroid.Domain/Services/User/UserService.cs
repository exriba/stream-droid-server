using Ardalis.GuardClauses;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;
using StreamDroid.Domain.RefreshPolicy;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Shared.Extensions;
using StreamDroid.Core.Exceptions;
using SharpTwitch.Auth;
using StreamDroid.Domain.DTOs;
using SharpTwitch.Helix;
using SharpTwitch.Core.Enums;
using StreamDroid.Core.Enums;

namespace StreamDroid.Domain.Services.User
{
    /// <summary>
    /// Default implementation of <see cref="IUserService"/>.
    /// </summary>
    public sealed class UserService : IUserService
    {
        private readonly IAuthApi _authApi;
        private readonly HelixApi _helixApi;
        private readonly IRepository<Entities.User> _repository;

        public UserService(HelixApi helixApi,
                           IAuthApi authApi,
                           IRepository<Entities.User> repository)
        {
            _authApi = authApi;
            _helixApi = helixApi;
            _repository = repository;
        }

        /// <inheritdoc/>
        public async Task<UserDto?> FindUserByIdAsync(string userId)
        {
            var user = await FetchUserByIdAsync(userId);
            return user is not null ? UserDto.FromEntity(user) : null;
        }

        /// <inheritdoc/>
        public async Task<UserDto> AuthenticateUserAsync(string code)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));

            var token = await _authApi.GetAccessTokenFromCodeAsync(code, CancellationToken.None);
            var userData = await _authApi.ValidateAccessTokenAsync(token.AccessToken, CancellationToken.None);
            var userDetailsTask = _helixApi.Users.GetUsersAsync(Array.Empty<string>(), token.AccessToken, CancellationToken.None);
            var userTask = FetchUserByIdAsync(userData.UserId);
            var userDetails = await userDetailsTask;
            var user = await userTask;

            var userBroadcasterType = userDetails.First().UserBroadcasterType;
            static UserType convert(BroadcasterType userBroadcasterType)
            {
                return userBroadcasterType switch
                {
                    BroadcasterType.NORMAL => UserType.NORMAL,
                    BroadcasterType.AFFILIATE => UserType.AFFILIATE,
                    BroadcasterType.PARTNER => UserType.PARTNER,
                    _ => throw new ArgumentException($"Invalid User Broadcaster Type ({userBroadcasterType})")
                };
            }

            if (user is null)
            {
                user = new Entities.User
                {
                    Id = userData.UserId,
                    Name = userData.Login,
                    UserType = convert(userBroadcasterType),
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken,
                };
                user = await _repository.AddAsync(user);
                return UserDto.FromEntity(user);
            }

            user.Name = userData.Login;
            user.AccessToken = token.AccessToken;
            user.RefreshToken = token.RefreshToken;
            user.UserType = convert(userBroadcasterType);
            user = await _repository.UpdateAsync(user);
            return UserDto.FromEntity(user);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task<Preferences> UpdateUserPreferencesAsync(string userId, Preferences preferences)
        {
            Guard.Against.Null(preferences, nameof(preferences));

            var user = await FetchUserAsync(userId);
            user.Preferences = preferences;
            user = await _repository.UpdateAsync(user);
            return user.Preferences;
        }

        #region Helpers
        /// <summary>
        /// Finds a user by the given id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>A user entity</returns>
        /// <exception cref="EntityNotFoundException">If the user is not found</exception>
        private async Task<Entities.User> FetchUserAsync(string userId)
        {
            return await FetchUserByIdAsync(userId) ?? throw new EntityNotFoundException(userId);
        }

        /// <summary>
        /// Finds a user by the given id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>A user entity.</returns>
        /// <exception cref="ArgumentNullException">If the user id is null</exception>
        /// <exception cref="ArgumentException">If the user id is an empty or whitespace string</exception>
        private async Task<Entities.User?> FetchUserByIdAsync(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

            return await _repository.FindByIdAsync(userId);
        }
        #endregion
    }
}
