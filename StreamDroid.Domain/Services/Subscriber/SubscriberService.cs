using Grpc.Core;
using Grpc.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using StreamDroid.Domain.Notification;
using StreamDroid.Domain.Services.Stream;
using System.Security.Claims;
using static GrpcEventService;

namespace StreamDroid.Domain.Services.Subscriber
{
    [Authorize]
    public sealed class SubscriberService : GrpcEventServiceBase
    {
        private readonly ITwitchSubscriber _twitchSubscriber;
        private readonly NotificationRegistry _notificationRegistry;
        private readonly ILogger<SubscriberService> _logger;

        public SubscriberService(ITwitchSubscriber twitchSubscriber,
                                 NotificationRegistry notificationRegistry,
                                 ILogger<SubscriberService> logger)
        {
            _twitchSubscriber = twitchSubscriber;
            _notificationRegistry = notificationRegistry;
            _logger = logger;
        }

        public override async Task Subscribe(Empty request, IServerStreamWriter<EventResponse> responseStream, ServerCallContext context)
        {
            var userPrincipal = context.GetHttpContext().User;
            var idClaim = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)!;
            var nameClaim = userPrincipal.FindFirst(ClaimTypes.Name)!;

            var reader = _notificationRegistry.Register(idClaim.Value);

            _logger.LogInformation("Initiating connection for client {userId} {name}.", idClaim.Value, nameClaim.Value);

            try
            {
                await _twitchSubscriber.SubscribeAsync(idClaim.Value, context.CancellationToken);

                await foreach (var notification in reader.ReadAllAsync(context.CancellationToken))
                {
                    var eventResponse = new EventResponse
                    {
                        Event = notification
                    };

                    await responseStream.WriteAsync(eventResponse, context.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Disconnecting client {userId} {name}.", idClaim.Value, nameClaim.Value);
                await _twitchSubscriber.UnsubscribeAsync(idClaim.Value, CancellationToken.None);
            }
            finally
            {
                _notificationRegistry.UnRegister(idClaim.Value);
            }
        }
    }
}
