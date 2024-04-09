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
        Task ConnectAsync();

        /// <summary>
        /// Registers a user and handler to listen for incoming events from twitch subscriptions.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="notificationHandler">notification handler</param>
        Task SubscribeAsync(string userId, Func<EventBase, Task> notificationHandler);

        /// <summary>
        /// Unregisters a user to stop listening for incoming events from twitch subscriptions.
        /// </summary>
        /// <param name="userId">user id</param>
        Task UnsubscribeAsync(string userId);
    }
}
