using Microsoft.AspNetCore.SignalR;
using StreamDroid.Application.Helpers;
using StreamDroid.Core.Entities;
using StreamDroid.Core.Enums;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure.Persistence;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace StreamDroid.Application.Services
{
    public class TwitchPubSubClient
    {
        // Replace this implementation
        private readonly TwitchPubSub _twitchPubSub;
        private readonly IAppSettings _appSettings;
        private readonly IList<string> _activeTopics;
        private readonly IHubContext<AssetHub> _hubContext;
        private readonly ILogger<TwitchPubSubClient> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TwitchPubSubClient(
             IAppSettings appSettings,
             IHubContext<AssetHub> hubContext,
             ILogger<TwitchPubSubClient> logger,
             IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _hubContext = hubContext;
            _appSettings = appSettings;
            _twitchPubSub = new TwitchPubSub();
            _activeTopics = new List<string>();
            _serviceScopeFactory = serviceScopeFactory;

            _twitchPubSub.OnStreamUp += OnStreamUp;
            _twitchPubSub.OnStreamDown += OnStreamDown;
            _twitchPubSub.OnListenResponse += OnListenResponse;
            _twitchPubSub.OnPubSubServiceError += OnPubSubServiceError;
            _twitchPubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            _twitchPubSub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        }

        public void Connect()
        {
            if (_activeTopics.Any())
                Disconnect();
            
            _twitchPubSub.Connect();
        }

        public void Disconnect() => _twitchPubSub.Disconnect();

        private void OnStreamUp(object sender, OnStreamUpArgs e)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            var users = uberRepository.Find<User>(u => u.Id.Equals(e.ChannelId));

            if (users.Any())
                _logger.LogInformation("{user} started streaming. Server time: {serverTime}", users.First().Name, e.ServerTime);
        }

        private void OnStreamDown(object sender, OnStreamDownArgs e)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            var users = uberRepository.Find<User>(u => u.Id.Equals(e.ChannelId));

            if (users.Any())
                _logger.LogInformation("{user} stopped streaming. Server time: {serverTime}", users.First().Name, e.ServerTime);
        }

        private void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
            {
                _activeTopics.Add(e.Topic);
                _logger.LogInformation("Connected to {topic} and listening for events.", e.Topic);
            }
            else
                _logger.LogError("Unable to connect to {topic} due to {reason}.", e.Topic, e.Response.Error);
        }

        private void OnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs e)
        {
            _logger.LogError("Exception occurred: {message}", e.Exception.Message);
        }

        private void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            var users = uberRepository.FindAll<User>();

            if (users.Any())
            {
                var user = users.First();

                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var token = userService.RefreshAccessToken(user).GetAwaiter().GetResult();
                _twitchPubSub.ListenToVideoPlayback(user.Id);
                _twitchPubSub.ListenToChannelPoints(user.Id);
                _twitchPubSub.SendTopics(token);
            }
        }

        private void OnChannelPointsRewardRedeemed(object? sender, OnChannelPointsRewardRedeemedArgs e)
        {
            var redemption = e.RewardRedeemed.Redemption;
            _logger.LogInformation("{user} redeemed {title} at {redeemedAt}.",
                redemption.User.DisplayName, redemption.Reward.Title, redemption.RedeemedAt);

            using var scope = _serviceScopeFactory.CreateScope();
            var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            var rewards = uberRepository.Find<Reward>(r => r.Id.Equals(e.RewardRedeemed.Redemption.Reward.Id));

            if (!rewards.Any())
                return;

            var reward = rewards.First();

            if (reward.Speech.Enabled)
            {
                var textToSpeechEvent = new
                {
                    reward.Speech.VoiceIndex,
                    Message = e.RewardRedeemed.Redemption.UserInput
                };

                _hubContext.Clients.All.SendAsync(Constants.TEXT_TO_SPEECH_EVENT, textToSpeechEvent).GetAwaiter().GetResult();
                return;
            }

            var asset = reward.GetRandomAsset();

            if (asset == null)
                return;

            var data = new
            {
                asset.Volume,
                Id = Guid.NewGuid().ToString(),
                AssetUri = new Uri(string.Join("/", _appSettings.StaticAssetUri, reward.Title, asset.ToString())),
            };

            if (asset.FileName.Extension.Equals(Extension.MP3))
                _hubContext.Clients.All.SendAsync(Constants.AUDIO_EVENT, data).GetAwaiter().GetResult();
            else
                _hubContext.Clients.All.SendAsync(Constants.VIDEO_EVENT, data).GetAwaiter().GetResult();
        }
    }
}
