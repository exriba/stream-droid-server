using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.RefreshPolicy;
using StreamDroid.Core.Exceptions;

namespace StreamDroid.Domain.Services.User
{
    /// <summary>
    /// Defines <see cref="Core.Entities.User"/> business logic.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Finds a user by the given id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>If found, a user DTO. Otherwise null.</returns>
        /// <exception cref="ArgumentNullException">If the user id is null</exception>
        /// <exception cref="ArgumentException">If the user id is an empty or whitespace string</exception>
        Task<UserDto?> FindUserByIdAsync(string userId);

        /// <summary>
        /// Authenticates the user for the given code.
        /// </summary>
        /// <param name="code">code</param>
        /// <returns>A user DTO.</returns>
        /// <exception cref="ArgumentNullException">If the code is null</exception>
        /// <exception cref="ArgumentException">If the code is an empty or whitespace string</exception>
        Task<UserDto> AuthenticateUserAsync(string code);

        /// <summary>
        /// Creates a token refresh policy for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>A token refresh policy</returns>
        /// <exception cref="ArgumentNullException">If the user id is null</exception>
        /// <exception cref="ArgumentException">If the user id is an empty or whitespace string</exception>
        /// <exception cref="EntityNotFoundException">If the user is not found</exception>
        Task<TokenRefreshPolicy> CreateTokenRefreshPolicyAsync(string userId);

        /// <summary>
        /// Updates user preferences by the given user id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="preferences">preferences</param>
        /// <returns>A preferences value object.</returns>
        /// <exception cref="ArgumentNullException">If the user id or preferences is null</exception>
        /// <exception cref="ArgumentException">If the user id is an empty or whitespace string</exception>
        /// <exception cref="EntityNotFoundException">If the user is not found</exception>
        Task<Preferences> UpdateUserPreferencesAsync(string userId, Preferences preferences);
    }
}
