using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.RefreshPolicy;

namespace StreamDroid.Domain.Services.User
{
    public interface IUserService
    {
        UserDto? FindById(string userId);
        Task<UserDto> Authenticate(string code);
        TokenRefreshPolicy CreateTokenRefreshPolicy(string userId);
        Preferences UpdatePreferences(string userId, Preferences preferences);
    }
}
