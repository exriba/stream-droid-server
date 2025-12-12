using Grpc.Model;
using System.Threading.Channels;

namespace StreamDroid.Domain.Services.Stream
{
    /// <summary>
    /// Lightweight component to achieve decoupled communication between two or more services within the same application.
    /// </summary>
    internal class NotificationService
    {
        private readonly Channel<NotificationEvent> _channel = Channel.CreateUnbounded<NotificationEvent>();

        public async ValueTask PublishNotificationAsync(NotificationEvent notification)
        {
            await _channel.Writer.WriteAsync(notification);
        }

        public ChannelReader<NotificationEvent> SubscribeToNotifications() => _channel.Reader;
    }
}