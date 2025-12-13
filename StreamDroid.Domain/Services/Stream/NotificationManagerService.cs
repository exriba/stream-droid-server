using Microsoft.Extensions.Hosting;

namespace StreamDroid.Domain.Services.Stream
{
    internal class NotificationManagerService : BackgroundService
    {
        private readonly NotificationService _notificationService;

        public NotificationManagerService(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var notificationHandler = _notificationService.SubscribeToNotifications();

            await foreach (var notificationEvent in notificationHandler.ReadAllAsync(cancellationToken))
            {
                if (SubscriberService.UserStreamWriters.TryGetValue(notificationEvent.StreamerId, out var tuple))
                {
                    var streamWriter = tuple.Item1;
                    var clientCancellationToken = tuple.Item2;

                    if (!clientCancellationToken.IsCancellationRequested)
                    {
                        await streamWriter.WriteAsync(new EventResponse
                        {
                            Event = notificationEvent,
                        }, cancellationToken);
                    }
                }
            }
        }
    }
}
