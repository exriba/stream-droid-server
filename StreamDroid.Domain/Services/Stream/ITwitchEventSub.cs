using StreamDroid.Domain.Services.Stream.Events;

namespace StreamDroid.Domain.Services.Stream
{
    /// <summary>
    /// Defines <see cref="SharpTwitch.EventSub.EventSub"/> business logic.
    /// </summary>
    public interface ITwitchEventSub : IAsyncDisposable
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
