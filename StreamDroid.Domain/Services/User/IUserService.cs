using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.RefreshPolicy;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.User
{
    public interface IUserService
    {
        Entities.User? FindById(string userId);
        Task<Entities.User> Authenticate(string code);
        TokenRefreshPolicy CreateTokenRefreshPolicy(string userId);
        Preferences UpdatePreferences(string userId, Preferences preferences);
    }
}
