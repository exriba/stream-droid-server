using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpTwitch.Core.Enums;
using SharpTwitch.EventSub;
using SharpTwitch.EventSub.Core.EventArgs;
using SharpTwitch.EventSub.Core.EventArgs.Channel.Redemption;
using SharpTwitch.EventSub.Core.EventArgs.Channel.Reward;
using SharpTwitch.EventSub.Core.EventArgs.Stream;
using SharpTwitch.EventSub.Core.EventMessageArgs;
using SharpTwitch.Helix;
using SharpTwitch.Helix.Models.Channel.Reward;
using StreamDroid.Core.Enums;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure.Persistence;
using System.Net.WebSockets;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Stream
{
    internal class TwitchEventSub : ITwitchEventSub
    {
        private const string AUDIO_EVENT = "AUDIO_EVENT";
        private const string VIDEO_EVENT = "VIDEO_EVENT";
        private const string TEXT_TO_SPEECH_EVENT = "TEXT_TO_SPEECH_EVENT";

        private static readonly IReadOnlySet<SubscriptionStatus> INACTIVE_SUBSCRIPTION_STATUS = new HashSet<SubscriptionStatus>
        {
            { SubscriptionStatus.AUTHORIZATION_REVOKED },
            { SubscriptionStatus.WEBSOCKET_DISCONNECTED },
            { SubscriptionStatus.WEBSOCKET_FAILED_PING_PONG },
            { SubscriptionStatus.WEBSOCKET_RECEIVED_INBOUND_TRAFFIC },
            { SubscriptionStatus.WEBSOCKET_CONNECTION_UNUSED },
            { SubscriptionStatus.WEBSOCKET_INTERNAL_ERROR },
            { SubscriptionStatus.WEBSOCKET_NETWORK_TIMEOUT },
            { SubscriptionStatus.WEBSOCKET_NETWORK_ERROR },
        };

        private static readonly IReadOnlySet<SubscriptionType> SUBSCRIPTION_TYPES = new HashSet<SubscriptionType>
        {
            { SubscriptionType.STREAM_OFFLINE },
            { SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_ADD },
            { SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_UPDATE },
            { SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_REMOVE },
            { SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_REDEMPTION_ADD },
        };

        private Entities.User _user = new();
        private readonly EventSub _eventSub;
        private readonly IAppSettings _appSettings;
        private readonly ILogger<TwitchEventSub> _logger;
        private readonly IHubContext<AssetHub> _hubContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private static readonly SemaphoreSlim connectSemaphore = new(1, 1);
        private static readonly SemaphoreSlim disconnectSemaphore = new(1, 1);

        public TwitchEventSub(EventSub eventSub,
                              IAppSettings appSettings,
                              IHubContext<AssetHub> hubContext,
                              IServiceScopeFactory serviceScopeFactory,
                              ILogger<TwitchEventSub> logger)
        {
            _logger = logger;
            _eventSub = eventSub;
            _hubContext = hubContext;
            _appSettings = appSettings;
            _serviceScopeFactory = serviceScopeFactory;

            _eventSub.OnRevocation += OnRevocation;
            _eventSub.OnErrorMessage += OnErrorMessage;
            _eventSub.OnStreamOnline += OnStreamOnline;
            _eventSub.OnStreamOffline += OnStreamOffline;
            _eventSub.OnClientConnected += OnClientConnected;
            _eventSub.OnClientDisconnected += OnClientDisconnected;
            _eventSub.OnCustomRewardAdd += OnCustomRewardAdd;
            _eventSub.OnCustomRewardUpdate += OnCustomRewardUpdate;
            _eventSub.OnCustomRewardRemove += OnCustomRewardRemove;
            _eventSub.OnChannelPointsCustomRewardRedemption += OnChannelPointsCustomRewardRedemption;
        }

        public async Task ConnectAsync(Entities.User user)
        {
            await connectSemaphore.WaitAsync();
            try
            {
                if (_eventSub.webSocketClient.Connected || user.UserType == UserType.NORMAL)
                    return;

                _user = user;
                await _eventSub.ConnectAsync();
            }
            finally
            {
                connectSemaphore.Release();
            }
        }

        public async Task DisconnectAsync(Entities.User user)
        {
            await disconnectSemaphore.WaitAsync();
            try
            {
                if (!_eventSub.webSocketClient.Connected || user.UserType == UserType.NORMAL)
                    return;

                await _eventSub.DisconnectAsync();
                await ClearSubscriptionAsync(user);
            }
            finally
            {
                disconnectSemaphore.Release();
            }
        }

        private async void OnClientConnected(object? sender, ClientConnectedArgs e)
        {
            _logger.LogInformation("EventSub client connected");

            if (!e.ReconnectionRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var helixApi = scope.ServiceProvider.GetRequiredService<HelixApi>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                var tokenRefreshPolicy = await userService.CreateTokenRefreshPolicyAsync(_user.Id);
                var helixSubscriptionResponse = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                    await helixApi.Subscriptions.CreateEventSubSubscriptionAsync(_user.Id, tokenRefreshPolicy.AccessToken, _eventSub.SessionId, SubscriptionType.STREAM_ONLINE, CancellationToken.None), tokenRefreshPolicy.ContextData);

                var tasks = SUBSCRIPTION_TYPES.Select(x =>
                    helixApi.Subscriptions.CreateEventSubSubscriptionAsync(_user.Id, tokenRefreshPolicy.AccessToken, _eventSub.SessionId, x, CancellationToken.None));

                await Task.WhenAll(tasks);
            }
        }

        private async void OnClientDisconnected(object? sender, ClientDisconnectedArgs e)
        {
            var reason = string.IsNullOrWhiteSpace(e.CloseStatusDescription) ? "N/A" : e.CloseStatusDescription;
            _logger.LogWarning("EventSub client disconnected. Status {status}. Reason {reason}.", e.WebSocketCloseStatus, reason);

            if (e.WebSocketCloseStatus is WebSocketCloseStatus.Empty)
            {
                await ClearSubscriptionAsync(_user);
                await _eventSub.ReconnectAsync();
            }
        }

        private void OnStreamOnline(object? sender, StreamOnlineArgs e)
        {
            var streamOnline = e.Notification.Payload.Event;
            _logger.LogInformation("{user} has gone online at {timeStamp}.", streamOnline.BroadcasterUserName, streamOnline.StartedAt);
        }

        private void OnStreamOffline(object? sender, StreamOfflineArgs e)
        {
            var streamOffline = e.Notification.Payload.Event;
            _logger.LogInformation("{user} has gone offline at {timeStamp}.", streamOffline.BroadcasterUserName, DateTime.Now);
        }

        private void OnRevocation(object? sender, RevocationArgs e)
        {
            _logger.LogWarning("Subscription revoked: {subscriptionType}.", e.SubscriptionType);

            switch (e.SubscriptionStatus)
            {
                case SubscriptionStatus.VERSION_REMOVED:
                    break;
                case SubscriptionStatus.USER_REMOVED: // Delete Subscription, remove handler and disconnect User 
                case SubscriptionStatus.AUTHORIZATION_REVOKED: // Delete Subscription, remove handler and disconnect User
                    break;
                default:
                    _logger.LogWarning("Unknown Subscription Status: {SubscriptionStatus}.", e.SubscriptionStatus);
                    break;
            }
        }

        private void OnErrorMessage(object? sender, ErrorMessageArgs e)
        {
            var message = string.IsNullOrWhiteSpace(e.Message) ? "N/A" : e.Message;
            _logger.LogError("EventSub ran into an error. Message {mesage}. Exception {exception}.", message, e.Exception);
        }

        private async void OnCustomRewardAdd(object? sender, CustomRewardAddArgs e)
        {
            var customReward = e.Notification.Payload.Event;
            _logger.LogInformation("Received custom reward add notification. Reward: {id} - {name}", customReward.Id, customReward.Title);
            await SaveReward(customReward);
        }

        private async void OnCustomRewardUpdate(object? sender, CustomRewardUpdateArgs e)
        {
            var customReward = e.Notification.Payload.Event;
            _logger.LogInformation("Received custom reward update notification. Reward: {id} - {name}", customReward.Id, customReward.Title);
            await SaveReward(customReward);
        }

        private async Task SaveReward(CustomReward customReward)
        {
            // var imageUrl = customReward.Image is null ? customReward.DefaultImage.Url1x : customReward.Image.Url1x;
            using var scope = _serviceScopeFactory.CreateScope();
            using var repository = scope.ServiceProvider.GetRequiredService<IRepository<Entities.Reward>>();
            var reward = await repository.FindByIdAsync(customReward.Id);

            if (reward is not null)
            {
                // reward.ImageUrl = imageUrl;
                reward.Title = customReward.Title;
                reward.Prompt = customReward.Prompt;
                reward.BackgroundColor = customReward.BackgroundColor;
                reward.Speech = new Speech(customReward.IsUserInputRequired, reward.Speech.VoiceIndex);
                await repository.UpdateAsync(reward);
            }
            else
            {
                reward = new Entities.Reward
                {
                    Id = customReward.Id,
                    // ImageUrl = imageUrl,
                    Title = customReward.Title,
                    Prompt = customReward.Prompt,
                    StreamerId = customReward.BroadcasterUserId,
                    BackgroundColor = customReward.BackgroundColor,
                    Speech = new Speech(customReward.IsUserInputRequired)
                };
                await repository.AddAsync(reward);
            }
        }

        private async void OnCustomRewardRemove(object? sender, CustomRewardRemoveArgs e)
        {
            var customReward = e.Notification.Payload.Event;
            _logger.LogInformation("Received custom reward delete notification. Reward: {id} - {name}", customReward.Id, customReward.Title);

            using var scope = _serviceScopeFactory.CreateScope();
            using var repository = scope.ServiceProvider.GetRequiredService<IRepository<Entities.Reward>>();
            await repository.DeleteAsync(customReward.Id);
        }

        private async void OnChannelPointsCustomRewardRedemption(object? sender, CustomRewardRedemptionArgs e)
        {
            var redeem = e.Notification.Payload.Event;
            _logger.LogInformation("Streamer {streamer}: {user} redeemed {title} at {redeemedAt}.",
                redeem.BroadcasterUserName, redeem.UserName, redeem.Reward.Title, redeem.RedeemedAt);

            using var scope = _serviceScopeFactory.CreateScope();
            using var rewardRepository = scope.ServiceProvider.GetRequiredService<IRepository<Entities.Reward>>();
            using var redemptionRepository = scope.ServiceProvider.GetRequiredService<IRedemptionRepository>();
            var reward = await rewardRepository.FindByIdAsync(redeem.Reward.Id);

            if (reward is null)
                return;

            var redemption = new Entities.Redemption
            {
                Id = Guid.NewGuid().ToString(),
                UserId = redeem.UserId,
                UserName = redeem.UserName,
                Reward = reward
            };

            await redemptionRepository.AddAsync(redemption);

            if (reward.Speech.Enabled)
            {
                var textToSpeechEvent = new
                {
                    reward.Speech.VoiceIndex,
                    Message = redeem.UserInput
                };

                await _hubContext.Clients.All.SendAsync(TEXT_TO_SPEECH_EVENT, textToSpeechEvent);
            }

            if (!reward.TryGetRandomAsset(out var asset)) 
                return;

            var data = new
            {
                asset!.Volume,
                Id = Guid.NewGuid().ToString(),
                AssetUri = new Uri(string.Join("/", _appSettings.StaticAssetUri, reward.Title, asset.ToString())),
            };

            if (asset.FileName.MediaExtension.Equals(MediaExtension.MP3))
                await _hubContext.Clients.All.SendAsync(AUDIO_EVENT, data);
            else
                await _hubContext.Clients.All.SendAsync(VIDEO_EVENT, data);
        }

        private async Task ClearSubscriptionAsync(Entities.User user)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var helixApi = scope.ServiceProvider.GetRequiredService<HelixApi>();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            var tokenRefreshPolicy = await userService.CreateTokenRefreshPolicyAsync(user.Id);
            var helixSubscriptionResponse = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                await helixApi.Subscriptions.GetEventSubSubscriptionAsync(user.Id, tokenRefreshPolicy.AccessToken, CancellationToken.None), tokenRefreshPolicy.ContextData);
            var inactiveSubscriptions = helixSubscriptionResponse.Data.Where(x => INACTIVE_SUBSCRIPTION_STATUS.Contains(x.SubscriptionStatus)).ToList();
                        
            var tasks = new List<Task>();
            foreach (var subscription in inactiveSubscriptions)
            {
                _logger.LogInformation("Deleting subscription with id {id} and type {type} that was created on {date}.", subscription.Id, subscription.Type, subscription.CreatedAt);
                var task = helixApi.Subscriptions.DeleteEventSubSubscriptionAsync(subscription.Condition.BroadcasterUserId, tokenRefreshPolicy.AccessToken, subscription.Id, CancellationToken.None);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
    }
}
