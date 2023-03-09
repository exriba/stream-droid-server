using Moq;
using SharpTwitch.Auth.Models;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Domain.Services.User;
using System.Text.Json;
using SharpTwitch.Auth;
using System.Text.Json.Nodes;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Tests.Services.User
{
    [Collection(TestCollectionFixture.Definition)]
    public class UserServiceTests
    {
        private readonly Mock<IAuthApi> _authApi;
        private readonly UserService _userService;
        private readonly TestFixture _testFixture;

        public UserServiceTests(TestFixture testFixture)
        {
            _testFixture = testFixture;
            _authApi = new Mock<IAuthApi>();
            _userService = new UserService(_authApi.Object, _testFixture.userRepository);
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

            _authApi.Setup(x => x.GetAccessTokenFromCodeAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(accessTokenResponse!));

            _authApi.Setup(x => x.ValidateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(validateTokenResponse!));

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
