using StreamDroid.Domain.Services.Stream.Events;

namespace StreamDroid.Domain.Services.Stream
{
    /// <summary>
    /// Defines <see cref="SharpTwitch.EventSub.EventSub"/> business logic.
    /// </summary>
    public interface ITwitchEventSub : IAsyncDisposable
    {
        /// <summary>
        /// Connects to twitch event sub.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <exception cref="ArgumentException">If the user is Normal</exception>
        Task ConnectAsync(string userId);

        /// <summary>
        /// Creates twitch subscriptions and adds a notification handler to start listening for incoming events for a given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="notificationHandler">notification handler</param>
        Task SubscribeAsync(string userId, Func<EventBase, Task> notificationHandler);

        /// <summary>
        /// Deletes twitch subscriptions and removes the notification handler for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="includeActive">include active subscriptions</param>
        Task UnsubscribeAsync(string userId, bool includeActiveSubscriptions = false);
    }
}
