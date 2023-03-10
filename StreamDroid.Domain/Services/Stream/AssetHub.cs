using Microsoft.AspNetCore.SignalR;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Stream
{
    public sealed class AssetHub : Hub
    {
        private const string ID = "id";

        private readonly ITwitchEventSub _twitchEventSub;
        private readonly IRepository<Entities.User> _repository;

        public AssetHub(ITwitchEventSub twitchEventSub,
                        IRepository<Entities.User> repository)
        {
            _repository = repository;
            _twitchEventSub = twitchEventSub;
        }

        // Create connection map from this to support multiple users
        public override async Task OnConnectedAsync()
        {
            var user = await ValidateUserKeyAsync();
            await _twitchEventSub.ConnectAsync(user);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = await ValidateUserKeyAsync();
            await _twitchEventSub.DisconnectAsync(user);
        }

        private async Task<Entities.User> ValidateUserKeyAsync()
        {
            var context = Context.GetHttpContext();

            if (!context!.Request.RouteValues.TryGetValue(ID, out object? id))
                throw new KeyNotFoundException("Invalid route: user key not found.");

            if (id is null || !Guid.TryParse(id.ToString(), out var userKey))
                throw new ArgumentException($"Invalid route: invalid user key ({id}).");

            var users = await _repository.FindAsync(u => u.UserKey.Equals(userKey));
            return users.Any() ? users.First() : throw new ArgumentException($"Invalid route: user key not found ({userKey}).");
        }
    }
}
