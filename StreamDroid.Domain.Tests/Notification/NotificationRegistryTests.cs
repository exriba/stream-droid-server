using Grpc.Model;
using StreamDroid.Domain.Notification;
using static Grpc.Model.NotificationEvent.Types;

namespace StreamDroid.Domain.Tests.Notification
{
    public class NotificationRegistryTests
    {
        private readonly NotificationRegistry _notificationRegistry = new();

        [Fact]
        public async Task NotificationRegistry_PublishAndRead()
        {
            var userId = Guid.NewGuid().ToString();
            var notificationEvent = new NotificationEvent
            {
                Id = Guid.NewGuid().ToString(),
                EventType = EventType.Audio,
                StreamerId = userId,
                AssetFileEvent = new AssetFileEvent
                {
                    Uri = "uri",
                    Volume = 100
                }
            };

            var channel = _notificationRegistry.Register(userId);

            await _notificationRegistry.Publish(userId, notificationEvent);

            channel.TryRead(out var notification);

            _notificationRegistry.UnRegister(userId);

            Assert.Equal(notificationEvent, notification);
        }
    }
}
