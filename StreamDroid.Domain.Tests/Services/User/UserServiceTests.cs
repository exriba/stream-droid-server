using Moq;
using SharpTwitch.Auth.Models;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Domain.Services.User;
using System.Text.Json;
using SharpTwitch.Auth;
using System.Text.Json.Nodes;

namespace StreamDroid.Domain.Tests.Services.User
{
    public class UserServiceTests : TestFixture
    {
        private readonly Mock<IAuthApi> _authApi;

        public UserServiceTests() : base("user-database.db")
        {
            _authApi = new Mock<IAuthApi>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void UserService_FindById_Throws_InvalidArgs(string userId)
        {
            var userService = new UserService(_authApi.Object, _uberRepository);

            Assert.ThrowsAny<ArgumentException>(() => userService.FindById(userId));
        }

        [Fact]
        public void UserService_FindById()
        {
            var user = CreateUser();

            var userService = new UserService(_authApi.Object, _uberRepository);
            var entity = userService.FindById(user.Id);

            Assert.Equal(user.Id, entity!.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void UserService_Authenticate_Throws_InvalidArgs(string code)
        {
            var userService = new UserService(_authApi.Object, _uberRepository);
            
            Assert.ThrowsAnyAsync<ArgumentException>(async () => await userService.Authenticate(code));
        }

        [Fact]
        public async Task UserService_Authenticate()
        {
            var user = CreateUser();
            var accessTokenResponseJson = new JsonObject
            {
                { "AccessToken", user.AccessToken },
                { "RefreshToken", user.RefreshToken }
            };
            var validateTokenResponseJson = new JsonObject
            {
                { "UserId", user.Id },
                { "Login", user.Name }
            };

            var accessTokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(accessTokenResponseJson.ToString());
            var validateTokenResponse = JsonSerializer.Deserialize<ValidateTokenResponse>(validateTokenResponseJson.ToString());

            _authApi.Setup(x => x.GetAccessTokenFromCodeAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(accessTokenResponse!));

            _authApi.Setup(x => x.ValidateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(validateTokenResponse!));

            var userService = new UserService(_authApi.Object, _uberRepository);
            var userDto = await userService.Authenticate(user.AccessToken);

            Assert.Equal(user.Id, user.Id);
            Assert.Equal(user.Name, user.Name);
        }

        [Fact]
        public void UserService_CreateTokenRefreshPolicy()
        {
            var user = CreateUser();

            var userService = new UserService(_authApi.Object, _uberRepository);
            var policy = userService.CreateTokenRefreshPolicy(user.Id);

            Assert.NotNull(policy.Policy);
            Assert.Equal(2, policy.ContextData.Keys.Count);
        }

        [Fact]
        public void UserService_UpdatePreferences_Throws_InvalidArgs()
        {
            var user = CreateUser();
            Preferences? preferences = null;

            var userService = new UserService(_authApi.Object, _uberRepository);

            Assert.ThrowsAny<ArgumentException>(() => userService.UpdatePreferences(user.Id, preferences));
        }

        [Fact]
        public void UserService_UpdatePreferences()
        {
            var user = CreateUser();
            var preferences = new Preferences();

            var userService = new UserService(_authApi.Object, _uberRepository);
            var data = userService.UpdatePreferences(user.Id, preferences);

            Assert.Equal(preferences, data);
        }

        private Core.Entities.User CreateUser()
        {
            var user = new Core.Entities.User
            {
                Id = Guid.NewGuid().ToString(),
                Name = "user",
                AccessToken = "accessToken",
                RefreshToken = "accessToken"
            };
             
            return _uberRepository.Save(user);
        }
    }
}
