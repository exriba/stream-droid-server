using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Models;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.User
{
    public interface IUserService
    {
        Entities.User? FindById(string userId);
        Task<Entities.User> Authenticate(string code);
        Task<string> RefreshAccessToken(Entities.User user);
        TokenRefreshPolicy CreateTokenRefreshPolicy(string userId);
        Preferences UpdatePreferences(string userId, Preferences preferences);
    }
}
