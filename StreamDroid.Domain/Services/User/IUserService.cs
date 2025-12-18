using StreamDroid.Domain.Policies;

namespace StreamDroid.Domain.Services.User
{
    /// <summary>
    /// Defines <see cref="Core.Entities.User"/> business logic.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Generates an access token for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>An access token</returns>
        Task<string> GenerateAccessTokenAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a token refresh policy for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>A token refresh policy</returns>
        Task<TokenRefreshPolicy> CreateTokenRefreshPolicyAsync(string userId, CancellationToken cancellationToken = default);
    }
}
