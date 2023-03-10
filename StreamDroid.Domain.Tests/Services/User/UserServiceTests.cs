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

namespace StreamDroid.Domain.Tests.Services.User
{
    [Collection(TestCollectionFixture.Definition)]
    public class UserServiceTests
    {
        private readonly Mock<IApiCore> _apiCore;
        private readonly Mock<IAuthApi> _authApi;
        private readonly UserService _userService;
        private readonly TestFixture _testFixture;

        public UserServiceTests(TestFixture testFixture)
        {
            _testFixture = testFixture;
            _authApi = new Mock<IAuthApi>();
            _apiCore = new Mock<IApiCore>();
            var coreSettings = new Mock<ICoreSettings>();
            var twitchEventSub = new Mock<ITwitchEventSub>();
            var helixApi = new HelixApi(coreSettings.Object, _apiCore.Object);
            _userService = new UserService(helixApi, _authApi.Object, twitchEventSub.Object, _testFixture.userRepository);
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
            var user = await CreateUser();

            var entity = await _userService.FindUserByIdAsync(user.Id);

            Assert.Equal(user.Id, entity!.Id);
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
            var user = await CreateUser();

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
            var user = await CreateUser();

            var policy = await _userService.CreateTokenRefreshPolicyAsync(user.Id);

            Assert.Equal(2, policy.ContextData.Keys.Count);
        }

        [Theory]
        [InlineData(null)]
        public async Task UserService_UpdateUserPreferencesAsync_Throws_InvalidArgs(Preferences preferences)
        {
            var user = await CreateUser();

            await Assert.ThrowsAnyAsync<ArgumentException>(async() => await _userService.UpdateUserPreferencesAsync(user.Id, preferences));
        }

        [Fact]
        public async Task UserService_UpdateUserPreferencesAsync()
        {
            var user = await CreateUser();
            var preferences = new Preferences();

            var data = await _userService.UpdateUserPreferencesAsync(user.Id, preferences);

            Assert.Equal(preferences, data);
        }

        private async Task<Entities.User> CreateUser()
        {
            var user = new Entities.User
            {
                Id = Guid.NewGuid().ToString(),
                Name = "user",
                AccessToken = "accessToken",
                RefreshToken = "accessToken"
            };
             
            return await _testFixture.userRepository.AddAsync(user);
        }
    }
}
