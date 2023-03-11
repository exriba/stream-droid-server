using Moq;
using SharpTwitch.Auth.Models;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Domain.Services.User;
using System.Text.Json;
using SharpTwitch.Auth;
using System.Text.Json.Nodes;
using Entities = StreamDroid.Core.Entities;
using StreamDroid.Domain.Services.Stream;
using SharpTwitch.Helix;
using SharpTwitch.Core.Settings;
using SharpTwitch.Core;
using SharpTwitch.Helix.Models;
using SharpTwitch.Core.Enums;
using HelixModels = SharpTwitch.Helix.Models;
using StreamDroid.Infrastructure.Persistence;

namespace StreamDroid.Domain.Tests.Services.User
{
    [Collection(TestCollectionFixture.Definition)]
    public class UserServiceTests
    {
        private readonly Mock<IApiCore> _apiCore;
        private readonly Mock<IAuthApi> _authApi;
        private readonly UserService _userService;
        private readonly IRepository<Entities.User> _userRepository;

        public UserServiceTests(TestFixture testFixture)
        {
            _authApi = new Mock<IAuthApi>();
            _apiCore = new Mock<IApiCore>();
            _userRepository = testFixture.userRepository;
            var coreSettings = new Mock<ICoreSettings>();
            var twitchEventSub = new Mock<ITwitchEventSub>();
            var helixApi = new HelixApi(coreSettings.Object, _apiCore.Object);
            _userService = new UserService(helixApi, _authApi.Object, twitchEventSub.Object, _userRepository);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task UserService_FindUserByIdAsync_Throws_InvalidArgs(string userId)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async() => await _userService.FindUserByIdAsync(userId));
        }

        [Fact]
        public async Task UserService_FindUserByIdAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var entity = await _userService.FindUserByIdAsync(id.ToString());

            Assert.Equal(id.ToString(), entity!.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void UserService_AuthenticateUserAsync_Throws_InvalidArgs(string code)
        {            
            Assert.ThrowsAnyAsync<ArgumentException>(async() => await _userService.AuthenticateUserAsync(code));
        }

        [Fact]
        public async Task UserService_AuthenticateUserAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            var user = await _userRepository.FindByIdAsync(id.ToString());

            var accessTokenResponseJson = new JsonObject
            {
                { "AccessToken", user.AccessToken },
                { "RefreshToken", user.RefreshToken }
            };

            var accessTokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(accessTokenResponseJson.ToString());

            var validateTokenResponseJson = new JsonObject
            {
                { "UserId", user.Id },
                { "Login", user.Name }
            };

            var validateTokenResponse = JsonSerializer.Deserialize<ValidateTokenResponse>(validateTokenResponseJson.ToString());;

            var helixUser = new SharpTwitch.Helix.Models.User.User
            {
                BroadcasterType = string.Empty
            };

            var helixUserCollectionResponse = new HelixCollectionResponse<HelixModels.User.User>
            {
                Data = new HelixModels.User.User[] { helixUser }
            };

            _authApi.Setup(x => x.GetAccessTokenFromCodeAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(accessTokenResponse!);
            _authApi.Setup(x => x.ValidateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(validateTokenResponse!);
            _apiCore.Setup(x => x.GetAsync<HelixCollectionResponse<HelixModels.User.User>>(
                    It.IsAny<UrlFragment>(),
                    It.IsAny<IDictionary<Header, string>>(),
                    It.IsAny<IEnumerable<KeyValuePair<QueryParameter, string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(helixUserCollectionResponse);

            var userDto = await _userService.AuthenticateUserAsync(user.AccessToken);

            Assert.Equal(user.Id, user.Id);
            Assert.Equal(user.Name, user.Name);
        }

        [Fact]
        public async Task UserService_CreateTokenRefreshPolicyAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var policy = await _userService.CreateTokenRefreshPolicyAsync(id.ToString());

            Assert.Equal(2, policy.ContextData.Keys.Count);
        }

        [Theory]
        [InlineData(null)]
        public async Task UserService_UpdateUserPreferencesAsync_Throws_InvalidArgs(Preferences preferences)
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            await Assert.ThrowsAnyAsync<ArgumentException>(async() => await _userService.UpdateUserPreferencesAsync(id.ToString(), preferences));
        }

        [Fact]
        public async Task UserService_UpdateUserPreferencesAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            var preferences = new Preferences();

            var data = await _userService.UpdateUserPreferencesAsync(id.ToString(), preferences);

            Assert.Equal(preferences, data);
        }

        private async Task SetupDataAsync(Guid id)
        {
            var user = new Entities.User
            {
                Id = id.ToString(),
                Name = "user",
                AccessToken = "accessToken",
                RefreshToken = "accessToken"
            };
             
            await _userRepository.AddAsync(user);
        }
    }
}
