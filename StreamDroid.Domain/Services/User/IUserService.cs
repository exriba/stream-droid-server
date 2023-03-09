using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.RefreshPolicy;

namespace StreamDroid.Domain.Services.User
{
    public interface IUserService
    {
        Task<UserDto?> FindUserByIdAsync(string userId);
        Task<UserDto> AuthenticateUserAsync(string code);
        Task<TokenRefreshPolicy> CreateTokenRefreshPolicyAsync(string userId);
        Task<Preferences> UpdateUserPreferencesAsync(string userId, Preferences preferences);
    }
}
