using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using StreamDroid.Domain.Middleware;
using StreamDroid.Domain.Services.User;
using System.Security.Claims;

namespace StreamDroid.Domain.Tests.Middleware
{
    public class AuthInterceptorTests
    {
        private const string TOKEN = "token";
        private const string EXPIRATION = "exp";

        private readonly AuthInterceptor _authInterceptor;

        public AuthInterceptorTests()
        {
            var mocklogger = new Mock<ILogger<AuthInterceptor>>();
            var mockUserManager = new Mock<IUserManager>();
            mockUserManager.Setup(x => x.GenerateAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(TOKEN));

            _authInterceptor = new AuthInterceptor(mockUserManager.Object, mocklogger.Object);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task AuthInterceptor_HandleRequest_InvalidClaims(bool idClaim, bool expClaim)
        {
            var empty = new Empty();
            var claimsIdentity = CreateClaimsIdentity(idClaim, expClaim);
            var context = CreateTestServerCallContext(claimsIdentity);

            await Assert.ThrowsAnyAsync<ArgumentException>(
                async () => await _authInterceptor.UnaryServerHandler(empty, context, Continuation)
            );
        }

        [Fact]
        public async Task AuthInterceptor_HandleRequest_InvalidExpiry()
        {
            var empty = new Empty();
            var claimsIdentity = CreateClaimsIdentity(true, false);
            claimsIdentity.AddClaim(new Claim(EXPIRATION, "x"));
            var context = CreateTestServerCallContext(claimsIdentity);

            await Assert.ThrowsAnyAsync<ArgumentException>(
                async () => await _authInterceptor.UnaryServerHandler(empty, context, Continuation)
            );
        }

        [Fact]
        public async Task AuthInterceptor_HandleRequest()
        {
            var empty = new Empty();
            var claimsIdentity = CreateClaimsIdentity();
            var context = CreateTestServerCallContext(claimsIdentity);

            var response = await _authInterceptor.UnaryServerHandler(empty, context, Continuation);

            Assert.Equal(empty, response);
        }

        #region Helpers
        private static Task<Empty> Continuation(Empty req, ServerCallContext ctx)
        {
            return Task.FromResult(req);
        }

        private static ClaimsIdentity CreateClaimsIdentity(bool includeId = true, bool includeExpiry = true)
        {
            var expiry = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds().ToString();

            var claimsIdentity = new ClaimsIdentity(
                authenticationType: "TestAuthentication"
            );

            if (includeId)
                claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));

            if (includeExpiry)
                claimsIdentity.AddClaim(new Claim(EXPIRATION, expiry));

            return claimsIdentity;
        }

        private static ServerCallContext CreateTestServerCallContext(ClaimsIdentity claimsIdentity)
        {
            var context = TestServerCallContext.Create(
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

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            context.UserState["__HttpContext"] = httpContext;

            return context;
        }
        #endregion
    }
}
