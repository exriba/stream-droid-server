using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Stream
{
    /// <summary>
    /// Asset event hub. 
    /// </summary>
    public sealed class AssetHub : Hub
    {
        private const string ID = "id";

        private readonly ITwitchEventSub _twitchEventSub;
        private readonly IRepository<Entities.User> _repository;
        private readonly ILogger<AssetHub> _logger;

        public AssetHub(ITwitchEventSub twitchEventSub,
                        IRepository<Entities.User> repository,
                        ILogger<AssetHub> logger)
        {
            _logger = logger;
            _repository = repository;
            _twitchEventSub = twitchEventSub;
        }

        /// TODO: Create connection map from this to support multiple users
        public override async Task OnConnectedAsync()
        {
            var user = await ValidateUserKeyAsync();
            await _twitchEventSub.ConnectAsync(user);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception is not null)
                _logger.LogError("An error ocurred: {exception}", exception);

            var user = await ValidateUserKeyAsync();
            await _twitchEventSub.DisconnectAsync(user);
        }

        /// <summary>
        /// Validates and finds the user for the current user key within the hub context.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">If no user can be found for the user key</exception>
        /// <exception cref="ArgumentException">If the user key is not a valid GUID or if the user is not found</exception>
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
