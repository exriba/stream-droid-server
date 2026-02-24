using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using StreamDroid.Core.Exceptions;

namespace StreamDroid.Domain.Middleware
{
    public class RequestInterceptor : Interceptor
    {
        private const string ID = "ID";
        private const string NAME = "Name";

        private readonly ILogger<RequestInterceptor> _logger;

        public RequestInterceptor(ILogger<RequestInterceptor> logger)
        {
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
            var correlationId = Guid.NewGuid();
            var userPrincipal = context.GetHttpContext().User;
            var authenticated = userPrincipal?.Identity?.IsAuthenticated ?? false;

            if (authenticated)
            {
                var idClaim = userPrincipal!.FindFirst(ID)!.Value;

                _logger.LogInformation("{correlationId}: Initiating request. {id} {userName} requested {method}.",
                    correlationId, idClaim, userPrincipal.Identity!.Name, context.Method);
            }
            else
            {
                _logger.LogInformation("{correlationId}: Initiating request. Client requested {method}.",
                    correlationId, context.Method);
            }

            try
            {
                return await next();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "{correlationId}: Operation canceled.", correlationId);
                throw new RpcException(new Status(StatusCode.Cancelled, "Operation was canceled."));
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogError(ex, "{correlationId}: Entity not found.", correlationId);
                throw new RpcException(new Status(StatusCode.NotFound, "Unable to find the data requested."));
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is DuplicateAssetException)
            {
                _logger.LogError(ex, "{correlationId}: Invalid Arguments.", correlationId);
                throw new RpcException(new Status(StatusCode.InvalidArgument, "An invalid argument was found while executing this request."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{correlationId}: Exception thrown.", correlationId);
                throw new RpcException(new Status(StatusCode.Internal, "An internal error occurred."));
            }
        }
    }
}
