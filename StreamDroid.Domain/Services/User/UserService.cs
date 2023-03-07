using Ardalis.GuardClauses;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;
using StreamDroid.Domain.RefreshPolicy;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Shared.Extensions;
using StreamDroid.Core.Exceptions;
using SharpTwitch.Auth;
using StreamDroid.Domain.DTOs;
using System.Text.Json;
using System.Text.Json.Nodes;
using StreamDroid.Core.Enums;

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

        public async Task<UserDto?> FindById(string userId)
        {
            var user = await FindUserById(userId);
            return user is not null ? UserDto.FromEntity(user) : null;
        }

        public async Task<UserDto> Authenticate(string code)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));

            var token = await _authApi.GetAccessTokenFromCodeAsync(code, CancellationToken.None);
            var userData = await _authApi.ValidateAccessTokenAsync(token.AccessToken, CancellationToken.None);
            var user = await FindUserById(userData.UserId) ?? new Entities.User { Id = userData.UserId };

            user.Name = userData.Login;
            user.AccessToken = token.AccessToken;
            user.RefreshToken = token.RefreshToken;
            user = await _uberRepository.Save(user);

            return UserDto.FromEntity(user);
        }

        public async Task<TokenRefreshPolicy> CreateTokenRefreshPolicy(string userId)
        {
            var user = await FindUser(userId);

            async Task<string> refreshToken(string userId)
            {
                var refreshToken = user.RefreshToken.Base64Decrypt();
                var token = await _authApi.RefreshAccessTokenAsync(refreshToken, CancellationToken.None);
                user.AccessToken = token.AccessToken;
                user.RefreshToken = token.RefreshToken;
                user = await _uberRepository.Save(user);
                return token.AccessToken;
            };

            var accessToken = user.AccessToken.Base64Decrypt();

            return new TokenRefreshPolicy(userId, accessToken, refreshToken);
        }

        public async Task<Preferences> UpdatePreferences(string userId, Preferences preferences)
        {
            Guard.Against.Null(preferences, nameof(preferences));

            var user = await FindUser(userId);
            user.Preferences = preferences;
            user = await _uberRepository.Save(user);

            return user.Preferences;
        }

        public async Task<JsonObject> DataExport(string userId)
        {
            var user = await FindUser(userId);
            var userDto = UserDto.FromEntity(user);
            var rewards = await _uberRepository.Find<Entities.Reward>(r => r.StreamerId.Equals(userId));
            var rewardsExt = rewards.Select(r =>
            {
                return new
                {
                    r.Id,
                    r.Title,
                    r.Prompt,
                    r.Speech,
                    r.ImageUrl,
                    r.BackgroundColor,
                    Assets = r.Assets.Select(a =>
                    {
                        return new
                        {
                            a.Volume,
                            FileName = new
                            {
                                a.FileName.Name,
                                Extension = a.FileName.Extension == Extension.MP3 ? MediaExtension.MP3.Name : MediaExtension.MP4.Name
                            }
                        };
                    })
                };
            });

            return new JsonObject
            {
                { "user", JsonSerializer.SerializeToNode(userDto) },
                { "rewards", JsonSerializer.SerializeToNode(rewardsExt) }
            };
        }

        private async Task<Entities.User> FindUser(string userId)
        {
            return await FindUserById(userId) ?? throw new EntityNotFoundException(userId);
        }

        private async Task<Entities.User?> FindUserById(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

            var users = await _uberRepository.Find<Entities.User>(u => u.Id.Equals(userId));
            return users.FirstOrDefault();
        }
    }
}
