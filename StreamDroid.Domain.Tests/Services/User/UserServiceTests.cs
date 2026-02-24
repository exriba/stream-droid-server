using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SharpTwitch.Auth;
using SharpTwitch.Auth.Models;
using SharpTwitch.Core;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Settings;
using SharpTwitch.Helix;
using SharpTwitch.Helix.Models;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Shared.Extensions;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Entities = StreamDroid.Core.Entities;
using HelixModels = SharpTwitch.Helix.Models;

namespace StreamDroid.Domain.Tests.Services.User
{
    [Collection(TestCollectionFixture.Definition)]
    public class UserServiceTests
    {
        private readonly Mock<IApiCore> _mockApiCore;
        private readonly Mock<IAuthApi> _mockAuthApi;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly Mock<IUberRepository> _mockRepository;

        private readonly UserService _userService;
        private readonly ServerCallContext _context = TestServerCallContext.Create(
            method: "TestMethod",
            host: "localhost",
            deadline: DateTime.UtcNow.AddMinutes(1),
            requestHeaders: [],
            cancellationToken: CancellationToken.None,
            peer: "127.0.0.1",
            authContext: null,
            contextPropagationToken: null,
            writeHeadersFunc: (m) => Task.CompletedTask,
            writeOptionsGetter: () => null,
            writeOptionsSetter: (o) => { }
        );

        public UserServiceTests(TestFixture testFixture)
        {
            _mockAuthApi = new Mock<IAuthApi>();
            _mockApiCore = new Mock<IApiCore>();
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockRepository = new Mock<IUberRepository>();

            var mockLogger = new Mock<ILogger<UserService>>();
            var mockCoreSettings = new Mock<ICoreSettings>();
            mockCoreSettings.SetupGet(x => x.ClientId).Returns("test");
            mockCoreSettings.SetupGet(x => x.RedirectUri).Returns("test");
            mockCoreSettings.SetupGet(x => x.Scopes).Returns([Scope.CHAT_READ]);

            var helixApi = new HelixApi(mockCoreSettings.Object, _mockApiCore.Object);
            var userManager = new UserManager(_mockAuthApi.Object, testFixture.options, _mockRepository.Object);
            _userService = new UserService(helixApi, _mockAuthApi.Object, mockCoreSettings.Object, _mockMemoryCache.Object, userManager, _mockRepository.Object, mockLogger.Object);
        }

        [Fact]
        public async Task UserService_GenerateLoginUrl()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

            var tcs = new TaskCompletionSource<string>() as object;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out tcs))
                .Returns(true);

            var response = await _userService.GenerateLoginUrl(sessionRequest, _context);

            Assert.Equal(sessionRequest.SessionId, response.SessionId);
        }

        [Fact]
        public async Task UserService_AuthenticateUser_SessionNotFound()
        {
            var id = Guid.NewGuid();
            var authenticationRequest = CreateAuthenticationRequest(id);

            var httpBody = await _userService.AuthenticateUser(authenticationRequest, _context);

            Assert.Equal("text/html", httpBody.ContentType);
        }

        [Fact]
        public async Task UserService_AuthenticateUser_ErrorOcurred()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

            var tcs = new TaskCompletionSource<string>() as object;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out tcs))
                .Returns(true);

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            var authenticationRequest = CreateAuthenticationRequest(id, hasError: true);

            var httpBody = await _userService.AuthenticateUser(authenticationRequest, _context);

            Assert.Equal("text/html", httpBody.ContentType);
        }

        [Fact]
        public async Task UserService_AuthenticateUser()
        {
            var id = Guid.NewGuid();
            var user = SetupUser();
            var sessionRequest = new SessionRequest
            {
                SessionId = user.Id,
            };

            var tcs = new TaskCompletionSource<string>() as object;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out tcs))
                .Returns(true);

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            ConfigureAuthApi();
            ConfigureHelixApi();

            var authenticationRequest = CreateAuthenticationRequest(id);

            _mockRepository.Setup(x => x.FindByIdAsync<Entities.User>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user)!);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Entities.User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user)!);

            var httpBody = await _userService.AuthenticateUser(authenticationRequest, _context);

            Assert.Equal("text/html", httpBody.ContentType);
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus_SessionNotFound()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

            var messages = new List<SessionStatus>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            await _userService.MonitorAuthenticationSessionStatus(sessionRequest, mockStreamWriter.Object, _context);

            Assert.Single(messages);
            Assert.Equal(SessionStatus.Types.Status.Error, messages.First().Status);
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus_CancelledByUser()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

            var tcs = new TaskCompletionSource<string>() as object;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out tcs))
                .Returns(true);

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            var source = new CancellationTokenSource();
            var context = CreateTestServerCallContext(source);

            var messages = new List<SessionStatus>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            source.CancelAfter(100);

            _ = _userService.MonitorAuthenticationSessionStatus(sessionRequest, mockStreamWriter.Object, context);

            source.Dispose();

            Assert.Empty(messages);
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus_CancelledByTimeout()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

            var tcs = new TaskCompletionSource<string>();
            var outvalue = tcs as object;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out outvalue))
                .Returns(true);

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            var messages = new List<SessionStatus>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            _ = _userService.MonitorAuthenticationSessionStatus(sessionRequest, mockStreamWriter.Object, _context);

            tcs.SetCanceled();

            Assert.Single(messages);
            Assert.Equal(SessionStatus.Types.Status.Error, messages.First().Status);
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus()
        {
            var user = SetupUser();
            var sessionRequest = new SessionRequest
            {
                SessionId = user.Id,
            };

            var tcs = new TaskCompletionSource<string>();
            tcs.SetResult(user.AccessToken);
            var outvalue = tcs as object;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out outvalue))
                .Returns(true);

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            ConfigureAuthApi();
            ConfigureHelixApi();

            _mockRepository.Setup(x => x.FindByIdAsync<Entities.User>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user)!);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Entities.User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));

            var messages = new List<SessionStatus>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);
            _ = _userService.MonitorAuthenticationSessionStatus(sessionRequest, mockStreamWriter.Object, _context);

            var authenticationRequest = CreateAuthenticationRequest(Guid.Parse(user.Id));
            await _userService.AuthenticateUser(authenticationRequest, _context);

            Assert.Single(messages);
            Assert.Equal(SessionStatus.Types.Status.Authorized, messages.First().Status);
        }

        [Fact]
        public async Task UserService_FindUser_Throws_EntityNotFound()
        {
            var user = SetupUser();
            ConfigureServerCallContext(user.Id);

            var request = new Google.Protobuf.WellKnownTypes.Empty();

            await Assert.ThrowsAnyAsync<EntityNotFoundException>(async () => await _userService.FindUser(request, _context));
        }

        [Fact]
        public async Task UserService_FindUser()
        {
            var user = SetupUser();
            ConfigureServerCallContext(user.Id);

            var request = new Google.Protobuf.WellKnownTypes.Empty();

            _mockRepository.Setup(x => x.FindByIdAsync<Entities.User>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user)!);

            var response = await _userService.FindUser(request, _context);

            Assert.Equal(user.Id, response.User.Id);
        }

        #region Helpers
        private static Entities.User SetupUser()
        {
            var id = Guid.NewGuid();

            return new Entities.User
            {
                Id = id.ToString(),
                Name = "user",
                AccessToken = "accessToken",
                RefreshToken = "refreshToken"
            };
        }

        private void ConfigureServerCallContext(string id)
        {
            var httpContext = new DefaultHttpContext();
            var claimsIdentity = new ClaimsIdentity();
            var claim = new Claim("Id", id);
            claimsIdentity.AddClaim(claim);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            httpContext.User = claimsPrincipal;
            _context.UserState["__HttpContext"] = httpContext;
        }

        private static AuthenticationRequest CreateAuthenticationRequest(Guid id, bool hasError = false)
        {
            var stringId = id.ToString();
            var encryptedState = stringId.Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);

            return new AuthenticationRequest
            {
                Code = stringId,
                State = encodedState,
                Error = hasError ? "Something went wrong." : string.Empty,
                ErrorDescription = hasError ? "Something went wrong." : string.Empty
            };
        }

        private void ConfigureAuthApi()
        {
            var user = SetupUser();

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

            var validateTokenResponse = JsonSerializer.Deserialize<ValidateTokenResponse>(validateTokenResponseJson.ToString());

            _mockAuthApi.Setup(x => x.GetAccessTokenFromCodeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessTokenResponse!);
            _mockAuthApi.Setup(x => x.ValidateAccessTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validateTokenResponse!);
        }

        private void ConfigureHelixApi()
        {
            var helixUser = new SharpTwitch.Helix.Models.User.User
            {
                BroadcasterType = string.Empty
            };

            var helixUserCollectionResponse = new HelixCollectionResponse<HelixModels.User.User>
            {
                Data = [helixUser]
            };

            _mockApiCore.Setup(x => x.GetAsync<HelixCollectionResponse<HelixModels.User.User>>(
                It.IsAny<UrlFragment>(),
                It.IsAny<IDictionary<Header, string>>(),
                It.IsAny<IEnumerable<KeyValuePair<QueryParameter, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(helixUserCollectionResponse);
        }

        private static Mock<IServerStreamWriter<SessionStatus>> CreateServerStreamWriterMock(List<SessionStatus> messages)
        {
            var mockStreamWriter = new Mock<IServerStreamWriter<SessionStatus>>();
            mockStreamWriter.Setup(x => x.WriteAsync(It.IsAny<SessionStatus>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback((SessionStatus status, CancellationToken token) => messages.Add(status));
            return mockStreamWriter;
        }

        private static ServerCallContext CreateTestServerCallContext(CancellationTokenSource source)
        {
            return TestServerCallContext.Create(
                method: "TestMethod",
                host: "localhost",
                deadline: DateTime.UtcNow.AddMinutes(1),
                requestHeaders: [],
                cancellationToken: source.Token,
                peer: "127.0.0.1",
                authContext: null,
                contextPropagationToken: null,
                writeHeadersFunc: (m) => Task.CompletedTask,
                writeOptionsGetter: () => null,
                writeOptionsSetter: (o) => { }
            );
        }
        #endregion
    }
}
