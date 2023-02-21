using Moq;
using SharpTwitch.Auth.Models;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Domain.Services.User;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Shared.Extensions;
using System.Linq.Expressions;
using System.Text.Json;
using SharpTwitch.Auth;
using System.Text.Json.Nodes;

namespace StreamDroid.Domain.Tests.Services.User
{
    public class UserServiceTests : TestFixture
    {
        private readonly Mock<IAuthApi> _authApi;
        private readonly Mock<IUberRepository> _uberRepository;
        private readonly Core.Entities.User _user;

        public UserServiceTests() : base()
        {
            _user = CreateUser();
            _authApi = new Mock<IAuthApi>();
            _uberRepository = new Mock<IUberRepository>();

            var users = new List<Core.Entities.User> { _user };
            _uberRepository.Setup(x => x.Find(It.IsAny<Expression<Func<Core.Entities.User, bool>>>())).Returns(users);
            _uberRepository.Setup(x => x.Save(It.IsAny<Core.Entities.User>())).Returns(_user);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void UserService_FindById_Throws_InvalidArgs(string userId)
        {
            var userService = new UserService(_authApi.Object, _uberRepository.Object);

            Assert.ThrowsAny<ArgumentException>(() => userService.FindById(userId));
        }

        [Fact]
        public void UserService_FindById()
        {
            var userService = new UserService(_authApi.Object, _uberRepository.Object);
            var user = userService.FindById(_user.Id);

            Assert.Equal(user!.Id, _user.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void UserService_Authenticate_Throws_InvalidArgs(string code)
        {
            var userService = new UserService(_authApi.Object, _uberRepository.Object);
            
            Assert.ThrowsAnyAsync<ArgumentException>(async () => await userService.Authenticate(code));
        }

        [Fact]
        public async Task UserService_Authenticate()
        {
            var token = "newToken";
            var accessTokenResponseJson = new JsonObject
            {
                { "AccessToken", token },
                { "RefreshToken", token }
            };
            var validateTokenResponseJson = new JsonObject
            {
                { "UserId", _user.Id },
                { "Login", _user.Name }
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

            var userService = new UserService(_authApi.Object, _uberRepository.Object);
            var user = await userService.Authenticate(token);

            Assert.Equal(user.Id, _user.Id);
            Assert.Equal(user.Name, _user.Name);
            Assert.True(user.AccessToken.IsBase64String());
            Assert.True(user.RefreshToken.IsBase64String());
        }

        [Fact]
        public void UserService_CreateTokenRefreshPolicy()
        {
            var userService = new UserService(_authApi.Object, _uberRepository.Object);
            var policy = userService.CreateTokenRefreshPolicy(_user.Id);

            Assert.NotNull(policy.Policy);
            Assert.Equal(2, policy.ContextData.Keys.Count);
        }

        [Fact]
        public void UserService_UpdatePreferences_Throws_InvalidArgs()
        {
            var userService = new UserService(_authApi.Object, _uberRepository.Object);
            
            Assert.ThrowsAny<ArgumentException>(() => userService.UpdatePreferences(_user.Id, null));
        }

        [Fact]
        public void UserService_UpdatePreferences()
        {
            var preferences = new Preferences();
            var userService = new UserService(_authApi.Object, _uberRepository.Object);
            var data = userService.UpdatePreferences(_user.Id, preferences);

            Assert.Equal(preferences, data);
        }

        private static Core.Entities.User CreateUser()
        {
            return new Core.Entities.User
            {
                Id = Guid.NewGuid().ToString(),
                Name = "user",
                AccessToken = "accessToken",
                RefreshToken = "accessToken"
            };
        }
    }
}
