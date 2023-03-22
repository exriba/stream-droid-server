using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Stream
{
    /// <summary>
    /// Defines <see cref="SharpTwitch.EventSub.EventSub"/> business logic.
    /// </summary>
    public interface ITwitchEventSub
    {
        /// <summary>
        /// Connects a user to twitch event sub.
        /// </summary>
        /// <param name="user">user</param>
        Task ConnectAsync(Entities.User user);

        /// <summary>
        /// Disconnects a user from twitch event sub.
        /// </summary>
        /// <param name="user">user</param>
        Task DisconnectAsync(Entities.User user);
    }
}
