using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharpTwitch.Core.Enums;
using StreamDroid.Application.Tests.Common;
using StreamDroid.Application.API.User;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using StreamDroid.Shared.Extensions;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Services.User;
using SharpTwitch.Core.Settings;
using StreamDroid.Domain.DTOs;

namespace StreamDroid.Application.Tests.API.User
{
    public class UserControllerTests : TestFixture
    {
        private const string ID = "Id";
        private const string REFERER = "Referer";
        private const string CLIENT_ID = "clientId";
        private const string REDIRECT_URI = "redirectUri";

        private readonly UserController _userController;
        private readonly Mock<IUserService> _mockUserService;

        public UserControllerTests() : base()
        {
            var id = Guid.NewGuid();
            var user = CreateUser(id);
            var claims = new List<Claim>
            {
                new Claim(ID, id.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims);

            var mockLogger = new Mock<ILogger<UserController>>();
            var mockCoreSettings = new Mock<ICoreSettings>();
            mockCoreSettings.Setup(x => x.ClientId).Returns(CLIENT_ID);
            mockCoreSettings.Setup(x => x.RedirectUri).Returns(REDIRECT_URI);
            mockCoreSettings.Setup(x => x.Scopes).Returns(new List<Scope> { Scope.BITS_READ } );

            _mockUserService = new Mock<IUserService>();
            _mockUserService.Setup(x => x.FindById(It.IsAny<string>())).ReturnsAsync(user);

            _userController = new UserController(_mockUserService.Object, mockCoreSettings.Object, mockLogger.Object)
            {
                ControllerContext = new ControllerContext()
            };
            _userController.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(claimsIdentity)
            };
            _userController.ControllerContext.HttpContext.Request.Headers.Add(REFERER, REFERER);
        }

        [Fact]
        public async Task UserController_Index()
        {
            var result = await _userController.Index();

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public async Task UserController_UpdatePreferences()
        {
            var preferences = new Preferences();

            var result = await _userController.UpdatePreferences(preferences);

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public void UserController_Login()
        {
            var result = _userController.Login();

            Assert.Equal(typeof(RedirectResult), result.GetType());
        }

        [Fact]
        public void UserController_AuthError()
        {
            var encryptedState = "state".Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);
            var result = _userController.AuthError("error", "errorDescription", encodedState);

            Assert.Equal(typeof(RedirectResult), result.GetType());
        }

        private static UserDto CreateUser(Guid id)
        {
            return new UserDto
            {
                Id = id.ToString(),
                Name = "Name"
            };
        }
    }
 }
