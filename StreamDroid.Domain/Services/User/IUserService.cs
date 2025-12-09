using StreamDroid.Core.Exceptions;
using StreamDroid.Domain.RefreshPolicy;

namespace StreamDroid.Domain.Services.User
{
    /// <summary>
    /// Defines <see cref="Core.Entities.User"/> business logic.
    /// </summary>
    public interface IUserService
    {
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
