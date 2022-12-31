using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpTwitch.Auth.Models;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Interfaces;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Domain.User;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Shared.Helpers;
using System.Linq.Expressions;

namespace StreamDroid.Domain.Tests.User
{
    public class UserServiceTests : TestFixture
    {
        private readonly Core.Entities.User _user;
        private readonly Mock<IApiCore> _apiCore;
        private readonly Mock<ICoreSettings> _coreSettings;
        private readonly Mock<IUberRepository> _uberRepository;

        public UserServiceTests() : base()
        {
            _user = CreateUser();
            _apiCore = new Mock<IApiCore>();
            _coreSettings = new Mock<ICoreSettings>();
            _uberRepository = new Mock<IUberRepository>();

            var users = new List<Core.Entities.User> { _user };
            _uberRepository.Setup(x => x.Find(It.IsAny<Expression<Func<Core.Entities.User, bool>>>())).Returns(users);
            _uberRepository.Setup(x => x.Save(It.IsAny<Core.Entities.User>())).Returns(_user);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void UserService_FindById_Throws_InvalidUserId(string userId)
        {
            var userService = new UserService(_apiCore.Object, _coreSettings.Object, _uberRepository.Object);
            Assert.Throws<ArgumentException>(() => userService.FindById(userId));
        }

        [Fact]
        public void UserService_FindById_Throws_Null()
        {
            var userService = new UserService(_apiCore.Object, _coreSettings.Object, _uberRepository.Object);
            Assert.Throws<ArgumentNullException>(() => userService.FindById(null));
        }

        [Fact]
        public void UserService_FindById()
        {
            var userService = new UserService(_apiCore.Object, _coreSettings.Object, _uberRepository.Object);
            var user = userService.FindById(_user.Id);

            Assert.Equal(user.Id, _user.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void UserService_Authenticate_Throws_InvalidCode(string code)
        {
            var userService = new UserService(_apiCore.Object, _coreSettings.Object, _uberRepository.Object);
            Assert.ThrowsAsync<ArgumentException>(async () => await userService.Authenticate(code));
        }

        [Fact]
        public void UserService_Authenticate_Throws_Null()
        {
            var userService = new UserService(_apiCore.Object, _coreSettings.Object, _uberRepository.Object);
            Assert.ThrowsAsync<ArgumentNullException>(async () => await userService.Authenticate(null));
        }

        [Fact]
        public async Task UserService_Authenticate()
        {
            var token = "newToken";
            var accessTokenResponseJson = new JObject
            {
                { "access_token", token },
                { "refresh_token", token }
            };

            var accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(accessTokenResponseJson.ToString());

            _apiCore.Setup(x => x.PostAsync<AccessTokenResponse>(
                    It.IsAny<UrlFragment>(), 
                    It.IsAny<IDictionary<Header, string>>(),
                    It.IsAny<FormUrlEncodedContent>(), 
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(accessTokenResponse!));

            var validateTokenResponseJson = new JObject
            {
                { "user_id", _user.Id },
                { "login", _user.Name }
            };

            var validateTokenResponse = JsonConvert.DeserializeObject<ValidateTokenResponse>(validateTokenResponseJson.ToString());

            _apiCore.Setup(x => x.GetAsync<ValidateTokenResponse>(
                    It.IsAny<UrlFragment>(),
                    It.IsAny<IDictionary<Header, string>>(),
                    It.IsAny<IDictionary<QueryParameter, string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(validateTokenResponse!));

            var userService = new UserService(_apiCore.Object, _coreSettings.Object, _uberRepository.Object);
            var user = await userService.Authenticate(token);

            Assert.Equal(user.Id, _user.Id);
            Assert.Equal(user.Name, _user.Name);
            Assert.True(user.AccessToken.IsBase64String());
            Assert.True(user.RefreshToken.IsBase64String());
        }

        [Fact]
        public void UserService_CreateTokenRefreshPolicy()
        {
            var userService = new UserService(_apiCore.Object, _coreSettings.Object, _uberRepository.Object);
            var policy = userService.CreateTokenRefreshPolicy(_user.Id);

            Assert.NotNull(policy.Policy);
            Assert.Equal(2, policy.ContextData.Keys.Count);
        }

        [Fact]
        public async Task UserService_RefreshAccessToken()
        {
            var accessToken = "access_token";
            var refreshTokenResponseJson = new JObject
            {
                { accessToken, "newToken" },
                { "refresh_token", "newToken" }
            };

            var refreshTokenResponse = JsonConvert.DeserializeObject<RefreshTokenResponse>(refreshTokenResponseJson.ToString());

            _apiCore.Setup(x => x.PostAsync<RefreshTokenResponse>(
                    It.IsAny<UrlFragment>(),
                    It.IsAny<IDictionary<Header, string>>(),
                    It.IsAny<FormUrlEncodedContent>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(refreshTokenResponse!));

            var userService = new UserService(_apiCore.Object, _coreSettings.Object, _uberRepository.Object);
            var token = await userService.RefreshAccessToken(_user);

            Assert.False(string.IsNullOrWhiteSpace(token));
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
