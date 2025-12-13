using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.AspNetCore.Http;
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
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Infrastructure.Persistence;
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
        private readonly Mock<IApiCore> _apiCore;
        private readonly Mock<IAuthApi> _authApi;
        private readonly UserService _userService;
        private readonly IRepository<Entities.User> _userRepository;
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
            _authApi = new Mock<IAuthApi>();
            _apiCore = new Mock<IApiCore>();
            _userRepository = testFixture.userRepository;
            var logger = new Mock<ILogger<UserService>>();
            var coreSettings = new Mock<ICoreSettings>();
            coreSettings.SetupGet(x => x.ClientId).Returns("test");
            coreSettings.SetupGet(x => x.RedirectUri).Returns("test");
            coreSettings.SetupGet(x => x.Scopes).Returns([Scope.CHAT_READ]);
            var helixApi = new HelixApi(coreSettings.Object, _apiCore.Object);
            _userService = new UserService(helixApi, _authApi.Object, _userRepository, testFixture.options,
                coreSettings.Object, logger.Object);
        }

        [Fact]
        public async Task UserService_GenerateLoginUrl()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

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

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            var authenticationRequest = CreateAuthenticationRequest(id, hasError: true);

            var httpBody = await _userService.AuthenticateUser(authenticationRequest, _context);

            Assert.Equal("text/html", httpBody.ContentType);
        }

        [Fact]
        public async Task UserService_AuthenticateUser()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            await ConfigureAuthApi(id);
            ConfigureHelixApi();

            var authenticationRequest = CreateAuthenticationRequest(id);

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
        public async Task UserService_MonitorAuthenticationSessionStatus_Cancelled()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            var source = new CancellationTokenSource();
            var context = CreateTestServerCallContext(source);

            var messages = new List<SessionStatus>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            source.CancelAfter(1000);

            _ = _userService.MonitorAuthenticationSessionStatus(sessionRequest, mockStreamWriter.Object, context);

            source.Dispose();

            Assert.Empty(messages);
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus()
        {
            var id = Guid.NewGuid();
            var sessionRequest = new SessionRequest
            {
                SessionId = id.ToString(),
            };

            await _userService.GenerateLoginUrl(sessionRequest, _context);

            await ConfigureAuthApi(id);
            ConfigureHelixApi();

            var messages = new List<SessionStatus>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);
            _ = _userService.MonitorAuthenticationSessionStatus(sessionRequest, mockStreamWriter.Object, _context);

            var authenticationRequest = CreateAuthenticationRequest(id);
            await _userService.AuthenticateUser(authenticationRequest, _context);

            Assert.Single(messages);
            Assert.Equal(SessionStatus.Types.Status.Authorized, messages.First().Status);
        }

        [Fact]
        public async Task UserService_FindUser_Throws_EntityNotFound()
        {
            var id = Guid.NewGuid();
            ConfigureServerCallContext(id);

            var request = new Google.Protobuf.WellKnownTypes.Empty();

            await Assert.ThrowsAnyAsync<EntityNotFoundException>(async () => await _userService.FindUser(request, _context));
        }

        [Fact]
        public async Task UserService_FindUser()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            ConfigureServerCallContext(id);

            var request = new Google.Protobuf.WellKnownTypes.Empty();

            var response = await _userService.FindUser(request, _context);

            Assert.Equal(id.ToString(), response.User.Id);
        }

        [Fact]
        public async Task UserService_CreateTokenRefreshPolicyAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var policy = await _userService.CreateTokenRefreshPolicyAsync(id.ToString());

            Assert.Equal(2, policy.ContextData.Keys.Count);
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

        private void ConfigureServerCallContext(Guid id)
        {
            var httpContext = new DefaultHttpContext();
            var claimsIdentity = new ClaimsIdentity();
            var claim = new Claim("Id", id.ToString());
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

        private async Task ConfigureAuthApi(Guid id)
        {
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

            var validateTokenResponse = JsonSerializer.Deserialize<ValidateTokenResponse>(validateTokenResponseJson.ToString()); ;

            _authApi.Setup(x => x.GetAccessTokenFromCodeAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(accessTokenResponse!);
            _authApi.Setup(x => x.ValidateAccessTokenAsync(
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

            _apiCore.Setup(x => x.GetAsync<HelixCollectionResponse<HelixModels.User.User>>(
                    It.IsAny<UrlFragment>(),
                    It.IsAny<IDictionary<Header, string>>(),
                    It.IsAny<IEnumerable<KeyValuePair<QueryParameter, string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(helixUserCollectionResponse);
        }

        private static Mock<IServerStreamWriter<SessionStatus>> CreateServerStreamWriterMock(List<SessionStatus> messages)
        {
            var mockStreamWriter = new Mock<IServerStreamWriter<SessionStatus>>();
            mockStreamWriter.Setup(x => x.WriteAsync(It.IsAny<SessionStatus>()))
                .Returns(Task.CompletedTask)
                .Callback<SessionStatus>(x => messages.Add(x));
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
    }
}
