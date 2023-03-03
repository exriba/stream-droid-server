using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.RefreshPolicy;

namespace StreamDroid.Domain.Services.User
{
    public interface IUserService
    {
        Task<UserDto?> FindById(string userId);
        Task<UserDto> Authenticate(string code);
        Task<TokenRefreshPolicy> CreateTokenRefreshPolicy(string userId);
        Task<Preferences> UpdatePreferences(string userId, Preferences preferences);
    }
}
