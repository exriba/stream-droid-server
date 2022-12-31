using Microsoft.AspNetCore.SignalR;
using StreamDroid.Core.Entities;
using StreamDroid.Infrastructure.Persistence;

namespace StreamDroid.Application.Services
{
    public sealed class AssetHub : Hub
    {
        private readonly IUberRepository _uberRepository;
        
        public AssetHub(IUberRepository uberRepository)
        {
            _uberRepository = uberRepository;
        }

        // Create connection map from this to support multiple users
        public override async Task OnConnectedAsync()
        {
            if (Context.GetHttpContext().Request.RouteValues.TryGetValue("id", out object? id))
            {
                if (Guid.TryParse(id.ToString(), out var guid))
                {
                    var users = _uberRepository.Find<User>(u => u.UserKey.Equals(guid.ToString()));

                    if (users.Any())
                    {
                        await base.OnConnectedAsync();
                        return;
                    }
                }
            }

            // Add more information
            throw new ArgumentException("Invalid url.");
        }
    }
}
