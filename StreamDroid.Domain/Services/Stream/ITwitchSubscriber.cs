using StreamDroid.Domain.Services.Stream.Events;

namespace StreamDroid.Domain.Services.Stream
{
    /// <summary>
    /// Defines methods to manage handlers for incoming notifications from <see cref="SharpTwitch.EventSub.EventSub"/>. 
    /// </summary>
    public interface ITwitchSubscriber
    {
        /// <summary>
        /// Creates twitch subscriptions and registers a notification handler for incoming events for a given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="notificationHandler">notification handler</param>
        Task SubscribeAsync(string userId, Func<EventBase, Task> notificationHandler);

        /// <summary>
        /// Unregister the notification handler for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        void UnsubscribeAsync(string userId);
    }
}
