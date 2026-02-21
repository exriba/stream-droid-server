using Grpc.Core;
using Grpc.Core.Interceptors;
using StreamDroid.Domain.Services.User;

namespace StreamDroid.Domain.Middleware
{
    public class AuthInterceptor : Interceptor
    {
        private const string ID = "Id";
        private const string EXPIRY = "exp";

        private readonly IUserManager _userManager;

        public AuthInterceptor(IUserManager userManager)
        {
            _userManager = userManager;
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

            if (authenticated)
            {
                var idClaim = userPrincipal!.FindFirst(ID)!.Value;
                var expClaim = userPrincipal.FindFirst(EXPIRY)!.Value;

                _ = long.TryParse(expClaim, out long unixSeconds);
                var expiry = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                var timeSpan = expiry.Subtract(DateTime.UtcNow);

                if (timeSpan.TotalSeconds < 300)
                {
                    var token = await _userManager.GenerateAccessTokenAsync(idClaim!);
                    context.ResponseTrailers.Add("access-token", token);
                }
            }

            return await next();
        }
    }
}
