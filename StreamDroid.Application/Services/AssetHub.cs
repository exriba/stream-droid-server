using Microsoft.AspNetCore.SignalR;
using StreamDroid.Core.Entities;
using StreamDroid.Infrastructure.Persistence;

namespace StreamDroid.Application.Services
{
    public sealed class AssetHub : Hub
    {
        private readonly IRepository<User> _repository;
        
        public AssetHub(IRepository<User> repository)
        {
            _repository = repository;
        }

        // Create connection map from this to support multiple users
        public override async Task OnConnectedAsync()
        {
            if (Context.GetHttpContext().Request.RouteValues.TryGetValue("id", out object? id))
            {
                if (Guid.TryParse(id.ToString(), out var guid))
                {
                    var users = await _repository.FindAsync(u => u.UserKey.Equals(guid));

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

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
