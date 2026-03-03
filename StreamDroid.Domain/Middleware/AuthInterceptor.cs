using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using StreamDroid.Domain.Services.User;

namespace StreamDroid.Domain.Middleware
{
    public class AuthInterceptor : Interceptor
    {
        private const string ID = "Id";
        private const string EXPIRY = "exp";
        private const string ACCESS_TOKEN = "access-token";

        private readonly IUserManager _userManager;
        private readonly ILogger<AuthInterceptor> _logger;

        public AuthInterceptor(IUserManager userManager, ILogger<AuthInterceptor> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            return await InvokeRequest(context, () => continuation(request, context));
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            return await InvokeRequest(context, () => continuation(requestStream, context));
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            await InvokeRequest<object>(context, async () =>
            {
                await continuation(request, responseStream, context);
                return null!;
            });
        }

        private async Task<TResponse> InvokeRequest<TResponse>(ServerCallContext context, Func<Task<TResponse>> next)
        {
            var userPrincipal = context.GetHttpContext().User;
            var authenticated = userPrincipal?.Identity?.IsAuthenticated ?? false;

            if (userPrincipal is not null && authenticated)
            {
                var idClaim = userPrincipal.FindFirst(ID);
                var expClaim = userPrincipal.FindFirst(EXPIRY);

                if (idClaim is null || expClaim is null)
                {
                    _logger.LogError("JWT missing required claims.");
                    throw new ArgumentException("Missing required JWT claims.");
                }

                var idValue = idClaim.Value;
                var expValue = expClaim.Value;

                if (!long.TryParse(expValue, out long unixSeconds))
                {
                    _logger.LogError("Invalid expiry format: {value}", expValue);
                    throw new ArgumentException("Invalid token expiry format.");
                }

                var expiry = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                var timeSpan = expiry - DateTimeOffset.UtcNow;

                if (timeSpan.TotalSeconds < 300)
                {
                    var token = await _userManager.GenerateAccessTokenAsync(idValue);
                    context.ResponseTrailers.Add(ACCESS_TOKEN, token);
                }
            }

            return await next();
        }
    }
}
