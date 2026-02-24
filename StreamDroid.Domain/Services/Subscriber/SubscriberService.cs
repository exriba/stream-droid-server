using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using StreamDroid.Domain.Notification;
using StreamDroid.Domain.Services.Stream;
using static GrpcEventService;

namespace StreamDroid.Domain.Services.Subscriber
{
    [Authorize]
    public sealed class SubscriberService : GrpcEventServiceBase
    {
        private const string ID = "Id";

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
            var claim = userPrincipal.Claims.First(c => c.Type.Equals(ID));
            var userName = userPrincipal.Identity!.Name;

            var reader = _notificationRegistry.Register(claim.Value);

            _logger.LogInformation("Initiating connection for client {userId} {name}.", claim.Value, userName);

            try
            {
                await _twitchSubscriber.SubscribeAsync(claim.Value, context.CancellationToken);

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
                _logger.LogInformation("Disconnecting client {userId} {name}.", claim.Value, userName);
                await _twitchSubscriber.UnsubscribeAsync(claim.Value, CancellationToken.None);
            }
            finally
            {
                _notificationRegistry.UnRegister(claim.Value);
            }
        }
    }
}
