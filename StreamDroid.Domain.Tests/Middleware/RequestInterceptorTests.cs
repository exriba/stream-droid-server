using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Middleware;
using System.Security.Claims;

namespace StreamDroid.Domain.Tests.Middleware
{
    public class RequestInterceptorTests
    {
        private const string OperationCanceledException = "OperationCanceledException";
        private const string EntityNotFoundException = "EntityNotFoundException";
        private const string ArgumentException = "ArgumentException";
        private const string DuplicateAssetException = "DuplicateAssetException";
        private const string Exception = "Exception";

        private readonly RequestInterceptor _requestInterceptor;

        public RequestInterceptorTests()
        {
            var mockLogger = new Mock<ILogger<RequestInterceptor>>();

            _requestInterceptor = new RequestInterceptor(mockLogger.Object);
        }

        [Fact]
        public async Task RequestInterceptor_HandleRequest_InvalidIdClaim()
        {
            var empty = new Empty();
            var context = CreateTestServerCallContext(false);

            await Assert.ThrowsAnyAsync<RpcException>(
                async () => await _requestInterceptor.UnaryServerHandler(empty, context, Continuation)
            );
        }

        [Theory]
        [InlineData(OperationCanceledException)]
        [InlineData(EntityNotFoundException)]
        [InlineData(ArgumentException)]
        [InlineData(DuplicateAssetException)]
        [InlineData(Exception)]
        public async Task RequestInterceptor_HandleRequest_ContinuationThrows(string input)
        {
            var context = CreateTestServerCallContext();

            await Assert.ThrowsAnyAsync<RpcException>(
                async () => await _requestInterceptor.UnaryServerHandler(input, context, Continuation)
            );
        }

        [Fact]
        public async Task RequestInterceptor_HandleRequest()
        {
            var empty = new Empty();
            var context = CreateTestServerCallContext();

            var response = await _requestInterceptor.UnaryServerHandler(empty, context, Continuation);

            Assert.Equal(empty, response);
        }

        #region Helpers
        private static Task<Empty> Continuation(Empty req, ServerCallContext ctx)
        {
            return Task.FromResult(req);
        }

        private static Task<string> Continuation(string input, ServerCallContext ctx)
        {
            return input switch
            {
                OperationCanceledException => throw new OperationCanceledException(),
                EntityNotFoundException => throw new EntityNotFoundException("1"),
                ArgumentException => throw new ArgumentException("arg"),
                DuplicateAssetException => throw new DuplicateAssetException(FileName.FromString("file.mp3")),
                Exception => throw new Exception(),
                _ => Task.FromResult(string.Empty),
            };
        }

        private static ServerCallContext CreateTestServerCallContext(bool includeId = true)
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

            var claimsIdentity = new ClaimsIdentity(
                authenticationType: "TestAuthentication"
            );

            if (includeId)
                claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));

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
