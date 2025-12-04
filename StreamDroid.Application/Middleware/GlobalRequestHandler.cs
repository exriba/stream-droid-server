using StreamDroid.Core.Exceptions;
using System.Net;

namespace StreamDroid.Application.Middleware
{
    /// <summary>
    /// Global middleware to handle request and exceptions
    /// </summary>
    public sealed class GlobalRequestHandler
    {
        private const string NAME = "Name";
        private const string CorrelationIdHeaderKey = "X-Correlation-ID";

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalRequestHandler> _logger;

        public GlobalRequestHandler(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<GlobalRequestHandler>();
        }

        /// <summary>
        /// Invokes http request delegates and handles exceptions globally. 
        /// </summary>
        /// <param name="context">http context</param>
        public async Task InvokeAsync(HttpContext context)
        {
            string? correlationId;

            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var correlationIds))
                correlationId = correlationIds.First(id => id.Equals(CorrelationIdHeaderKey));
            else
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers.Append(CorrelationIdHeaderKey, correlationId);
            }

            if (context.User.Claims.Any())
            {
                var claim = context.User.Claims.First(c => c.Type.Equals(NAME));
                _logger.LogInformation("{correlationId}: {user} requested {method} {url}",
                     correlationId, claim.Value, context.Request?.Method, context.Request?.Path.Value);
            }
            else
                _logger.LogInformation("{correlationId}: {referer} requested {method} {url}",
                    correlationId, context.Request?.Headers.Referer, context.Request?.Method, context.Request?.Path.Value);

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.TryGetValue(CorrelationIdHeaderKey, out var correlationIds))
                    context.Response.Headers.Append(CorrelationIdHeaderKey, correlationId);
                return Task.CompletedTask;
            });

            try
            {
                await _next(context);
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is EntityNotFoundException ||
                                       ex is DuplicateAssetException)
            {
                _logger.LogError("{correlationId}: Error - {message}", correlationId, ex.Message);
                await HandleException(context, ex.Message, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("{correlationId}: Exception thrown. Message: {message}", correlationId, ex.Message);
                await HandleException(context, "Something went wrong.", HttpStatusCode.InternalServerError);
            }
        }

        private static async Task HandleException(HttpContext context, string message, HttpStatusCode statusCode)
        {
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(message);
        }
    }
}
