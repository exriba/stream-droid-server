using Grpc.Model;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace StreamDroid.Domain.Notification
{
    /// <summary>
    /// Lightweight component to achieve decoupled communication between two or more services within the same application.
    /// </summary>
    public class NotificationRegistry
    {
        private readonly ConcurrentDictionary<string, Channel<NotificationEvent>> _connections = new();

        /// <summary>
        /// Registers a user to receive notifications and creates a channel associated with the specified user id.
        /// </summary>
        /// <param name="userId">the user id</param>
        /// <returns><see cref="ChannelReader{T}"/> that can be used to asynchronously read notifications for the specified user.</returns>
        public ChannelReader<NotificationEvent> Register(string userId)
        {
            var channel = Channel.CreateUnbounded<NotificationEvent>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                }
            );

            _connections[userId] = channel;

            return channel.Reader;
        }

        /// <summary>
        /// Unregisters the specified user and removes their associated notification channel from the registry.
        /// </summary>
        /// <param name="userId">the user id</param>
        public void UnRegister(string userId)
        {
            if (_connections.TryRemove(userId, out var channel))
            {
                channel.Writer.TryComplete();
            }
        }

        /// <summary>
        /// Publishes a notification to the specified user's channel.
        /// </summary>
        /// <param name="userId">the user id</param>
        /// <param name="notification">the notification</param>
        public async Task Publish(string userId, NotificationEvent notification)
        {
            if (_connections.TryGetValue(userId, out var channel))
                await channel.Writer.WriteAsync(notification);
        }
    }
}
