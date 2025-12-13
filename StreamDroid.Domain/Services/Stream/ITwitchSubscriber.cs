namespace StreamDroid.Domain.Services.Stream
{
    public interface ITwitchSubscriber
    {
        /// <summary>
        /// Creates subscriptions for the given user id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="cancellationToken">cancellation token</param>
        Task SubscribeAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes subscriptions for the given user id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="cancellationToken">cancellation token</param>
        Task UnsubscribeAsync(string userId, CancellationToken cancellationToken = default);
    }
}
