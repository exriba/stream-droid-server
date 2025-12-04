using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Settings;
using StreamDroid.Application.API.User;
using StreamDroid.Application.Settings;
using StreamDroid.Application.Tests.Common;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Services.User;
using StreamDroid.Shared.Extensions;
using System.Security.Claims;

namespace StreamDroid.Application.Tests.API.User
{
    public class UserControllerTests : IClassFixture<TestFixture>, IDisposable
    {
        private const string ID = "Id";
        private const string REFERER = "Referer";
        private const string CLIENT_ID = "clientId";
        private const string REDIRECT_URI = "redirectUri";

        private readonly UserController _userController;
        private readonly Mock<IUserService> _mockUserService;

        public UserControllerTests()
        {
            var id = Guid.NewGuid();
            var user = CreateUser(id);
            var claims = new List<Claim>
            {
                new(ID, id.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims);

            var mockLogger = new Mock<ILogger<UserController>>();
            var mockCoreSettings = new Mock<ICoreSettings>();
            mockCoreSettings.Setup(x => x.ClientId).Returns(CLIENT_ID);
            mockCoreSettings.Setup(x => x.RedirectUri).Returns(REDIRECT_URI);
            mockCoreSettings.Setup(x => x.Scopes).Returns([Scope.BITS_READ]);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            mockJwtSettings.Setup(x => x.Value).Returns(new JwtSettings
            {
                SigningKey = "averylongkeyusedforsigningandvalidatingjsonwebtokens",
                Audience = "test",
                Issuer = "test",
            });

            _mockUserService = new Mock<IUserService>();
            _mockUserService.Setup(x => x.FindUserByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _mockUserService.Setup(x => x.AuthenticateUserAsync(It.IsAny<string>())).ReturnsAsync(user);

            _userController = new UserController(_mockUserService.Object, mockCoreSettings.Object, mockJwtSettings.Object, mockLogger.Object)
            {
                ControllerContext = new ControllerContext()
            };
            _userController.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(claimsIdentity)
            };
            _userController.ControllerContext.HttpContext.Request.Headers.Append(REFERER, REFERER);
        }

        [Fact]
        public async Task UserController_FindUserByIdAsync()
        {
            var result = await _userController.FindUserByIdAsync();

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public async Task UserController_UpdateUserPreferencesAsync()
        {
            var preferences = new Preferences();

            var result = await _userController.UpdateUserPreferencesAsync(preferences);

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public void UserController_Login()
        {
            var result = _userController.Login();

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public async Task UserController_AuthenticationSuccessAsync()
        {
            var encryptedState = "state".Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);
            var result = await _userController.AuthenticationSuccessAsync("code", encodedState);

            Assert.Equal(typeof(RedirectResult), result.GetType());
        }

        [Fact]
        public void UserController_AuthenticationError()
        {
            var encryptedState = "state".Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);
            var result = _userController.AuthenticationError("error", "errorDescription", encodedState);

            Assert.Equal(typeof(RedirectResult), result.GetType());
        }

        private static UserDto CreateUser(Guid id)
        {
            return new UserDto
            {
                Id = id.ToString(),
                Name = "Name",
                UserKey = Guid.NewGuid(),
                Preferences = new Preferences()
            };
        }

        public void Dispose()
        {
            _userController.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
