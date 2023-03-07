using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.RefreshPolicy;
using System.Text.Json.Nodes;

namespace StreamDroid.Domain.Services.User
{
    public interface IUserService
    {
        Task<UserDto?> FindById(string userId);
        Task<UserDto> Authenticate(string code);
        Task<TokenRefreshPolicy> CreateTokenRefreshPolicy(string userId);
        Task<Preferences> UpdatePreferences(string userId, Preferences preferences);

        Task<JsonObject> DataExport(string userId);
    }
}
