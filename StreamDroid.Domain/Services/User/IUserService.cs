using StreamDroid.Core.Exceptions;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.RefreshPolicy;

namespace StreamDroid.Domain.Services.User
{
    /// <summary>
    /// Defines <see cref="Core.Entities.User"/> business logic.
    /// </summary>
    public interface IUserService
    {
        // TODO: Remove this method once grpc is fully implemented.
        /// <summary>
        /// Finds a collection of users.
        /// </summary>
        /// <returns>A collection of user DTOs.</returns>
        Task<IReadOnlyCollection<UserDto>> FindUsersAsync();

        /// <summary>
        /// Creates a token refresh policy for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>A token refresh policy</returns>
        /// <exception cref="ArgumentNullException">If the user id is null</exception>
        /// <exception cref="ArgumentException">If the user id is an empty or whitespace string</exception>
        /// <exception cref="EntityNotFoundException">If the user is not found</exception>
        Task<TokenRefreshPolicy> CreateTokenRefreshPolicyAsync(string userId);
    }
}
