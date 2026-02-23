using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using StreamDroid.Application.Tests.Common;
using StreamDroid.Shared.Extensions;

namespace StreamDroid.Application.Tests.Services.User
{
    [Collection(TestCollectionFixture.Definition)]
    public class UserServiceTests
    {
#pragma warning disable CS0436 // Type conflicts with imported type
        private readonly GrpcUserService.GrpcUserServiceClient _grpcUserServiceClient;
        private readonly string _userId;

        public UserServiceTests(TestFixture testFixture)
        {
            _userId = testFixture.userId;
            _grpcUserServiceClient = new GrpcUserService.GrpcUserServiceClient(testFixture.grpcChannel);
        }

        [Fact]
        public async Task UserService_GenerateLoginUrl()
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionRequest = new SessionRequest
            {
                SessionId = sessionId,
            };

            var response = await _grpcUserServiceClient.GenerateLoginUrlAsync(sessionRequest);

            Assert.Equal(sessionId, response.SessionId);
        }

        [Fact]
        public async Task UserService_AuthenticateUser_SessionNotFound()
        {
            var state = "Test";
            var encryptedState = state.Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);

            var authenticationRequest = new AuthenticationRequest
            {
                State = encodedState
            };

            var response = await _grpcUserServiceClient.AuthenticateUserAsync(authenticationRequest);

            Assert.Equal("text/html", response.ContentType);
        }

        [Fact]
        public async Task UserService_AuthenticateUser_ErrorOcurred()
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionRequest = new SessionRequest
            {
                SessionId = sessionId,
            };

            await _grpcUserServiceClient.GenerateLoginUrlAsync(sessionRequest);

            var encryptedState = sessionId.Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);

            var authenticationRequest = new AuthenticationRequest
            {
                State = encodedState,
                Error = "Error"
            };

            var response = await _grpcUserServiceClient.AuthenticateUserAsync(authenticationRequest);

            Assert.Equal("text/html", response.ContentType);
        }

        [Fact]
        public async Task UserService_AuthenticateUser()
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionRequest = new SessionRequest
            {
                SessionId = sessionId,
            };

            await _grpcUserServiceClient.GenerateLoginUrlAsync(sessionRequest);

            var encryptedState = sessionId.Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);

            var authenticationRequest = new AuthenticationRequest
            {
                State = encodedState,
                Code = "code"
            };

            var response = await _grpcUserServiceClient.AuthenticateUserAsync(authenticationRequest);

            Assert.Equal("text/html", response.ContentType);
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus_Error()
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionRequest = new SessionRequest
            {
                SessionId = sessionId,
            };

            await foreach (var response in _grpcUserServiceClient.MonitorAuthenticationSessionStatus(sessionRequest).ResponseStream.ReadAllAsync())
            {
                Assert.Equal(SessionStatus.Types.Status.Error, response.Status);
            }
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus_CancelledByUser()
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionRequest = new SessionRequest
            {
                SessionId = sessionId,
            };

            await _grpcUserServiceClient.GenerateLoginUrlAsync(sessionRequest);

            var exception = await Assert.ThrowsAnyAsync<RpcException>(
                async () =>
                {
                    var cts = new CancellationTokenSource();
                    var streamingResponse = _grpcUserServiceClient.MonitorAuthenticationSessionStatus(sessionRequest, cancellationToken: cts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                    cts.Cancel();
                    await foreach (var response in streamingResponse.ResponseStream.ReadAllAsync()) { }
                    cts.Dispose();
                }
            );
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus_UnauthorizedAccess()
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionRequest = new SessionRequest
            {
                SessionId = sessionId,
            };

            await _grpcUserServiceClient.GenerateLoginUrlAsync(sessionRequest);

            var streamingResponse = _grpcUserServiceClient.MonitorAuthenticationSessionStatus(sessionRequest);

            var encryptedState = sessionId.Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);

            var authenticationRequest = new AuthenticationRequest
            {
                State = encodedState,
                Error = "UnauthorizedAccess"
            };

            await _grpcUserServiceClient.AuthenticateUserAsync(authenticationRequest);

            await foreach (var response in streamingResponse.ResponseStream.ReadAllAsync())
            {
                Assert.Equal(SessionStatus.Types.Status.Error, response.Status);
            }
        }

        [Fact]
        public async Task UserService_MonitorAuthenticationSessionStatus_Authorized()
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionRequest = new SessionRequest
            {
                SessionId = sessionId,
            };

            await _grpcUserServiceClient.GenerateLoginUrlAsync(sessionRequest);

            var encryptedState = sessionId.Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);

            var authenticationRequest = new AuthenticationRequest
            {
                State = encodedState,
                Code = "code"
            };

            var streamingResponse = _grpcUserServiceClient.MonitorAuthenticationSessionStatus(sessionRequest);

            await _grpcUserServiceClient.AuthenticateUserAsync(authenticationRequest);

            await foreach (var response in streamingResponse.ResponseStream.ReadAllAsync())
            {
                Assert.Equal(SessionStatus.Types.Status.Authorized, response.Status);
            }
        }

        [Fact]
        public async Task UserService_FindUser()
        {
            var request = new Empty();

            var response = await _grpcUserServiceClient.FindUserAsync(request);

            Assert.Equal(_userId, response.User.Id);
        }
#pragma warning restore CS0436 // Type conflicts with imported type
    }
}
