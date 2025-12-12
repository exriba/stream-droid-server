namespace StreamDroid.Domain.Services.Stream
{
    public interface ITwitchSubscriber
    {
        /// <summary>
        /// Creates subscriptions for the given user id.
        /// </summary>
        /// <param name="userId">user id</param>
        Task SubscribeAsync(string userId);

        /// <summary>
        /// Removes subscriptions for the given user id.
        /// </summary>
        /// <param name="userId">user id</param>
        Task UnsubscribeAsync(string userId);
    }
}
