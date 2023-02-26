using Microsoft.AspNetCore.SignalR;
using SharpTwitch.Core.Enums;
using SharpTwitch.EventSub;
using SharpTwitch.EventSub.Core.EventArgs;
using SharpTwitch.EventSub.Core.EventArgs.Channel.Redemption;
using SharpTwitch.EventSub.Core.EventArgs.Channel.Reward;
using SharpTwitch.EventSub.Core.EventArgs.Stream;
using SharpTwitch.EventSub.Core.EventMessageArgs;
using SharpTwitch.Helix;
using StreamDroid.Core.Entities;
using StreamDroid.Core.Enums;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Core.ValueObjects;
using SharpTwitch.Helix.Models.Channel.Reward;

namespace StreamDroid.Application.Services
{
    public class EventSubHostedService : IHostedService // fixme: review this - move to domain module
    {
        private const string AUDIO_EVENT = "AUDIO_EVENT";
        private const string VIDEO_EVENT = "VIDEO_EVENT";
        private const string TEXT_TO_SPEECH_EVENT = "TEXT_TO_SPEECH_EVENT";

        private readonly EventSub _eventSub;
        private readonly HelixApi _helixApi;
        private readonly UserDetails _userDetails;
        private readonly IAppSettings _appSettings;
        private readonly IHubContext<AssetHub> _hubContext;
        private readonly ILogger<EventSubHostedService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ISet<SubscriptionStatus> _inactiveSubscriptionStatus;

        public EventSubHostedService(EventSub eventSub, 
                                     HelixApi helixApi,
                                     IAppSettings appSettings,
                                     IHubContext<AssetHub> hubContext, 
                                     IServiceScopeFactory serviceScopeFactory, 
                                     ILogger<EventSubHostedService> logger)
        {
            _logger = logger;
            _eventSub = eventSub;
            _helixApi = helixApi;
            _hubContext = hubContext;
            _appSettings = appSettings;
            _userDetails = new UserDetails();
            _serviceScopeFactory = serviceScopeFactory;
            _inactiveSubscriptionStatus = new HashSet<SubscriptionStatus>
            {
                { SubscriptionStatus.WEBSOCKET_DISCONNECTED },
                { SubscriptionStatus.WEBSOCKET_FAILED_PING_PONG },
                { SubscriptionStatus.WEBSOCKET_RECEIVED_INBOUND_TRAFFIC },
                { SubscriptionStatus.WEBSOCKET_CONNECTION_UNUSED },
            };

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

        private async void OnClientConnected(object? sender, ClientConnectedArgs e)
        {
            _logger.LogInformation("EventSub client connected");

            if (!e.ReconnectionRequested)
            {
                await _helixApi.Subscriptions.CreateEventSubSubscriptionAsync(_userDetails.UserId, _userDetails.AccessToken, _eventSub.SessionId, SubscriptionType.STREAM_ONLINE, CancellationToken.None);
                await _helixApi.Subscriptions.CreateEventSubSubscriptionAsync(_userDetails.UserId, _userDetails.AccessToken, _eventSub.SessionId, SubscriptionType.STREAM_OFFLINE, CancellationToken.None);
                await _helixApi.Subscriptions.CreateEventSubSubscriptionAsync(_userDetails.UserId, _userDetails.AccessToken, _eventSub.SessionId, SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_ADD, CancellationToken.None);
                await _helixApi.Subscriptions.CreateEventSubSubscriptionAsync(_userDetails.UserId, _userDetails.AccessToken, _eventSub.SessionId, SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_UPDATE, CancellationToken.None);
                await _helixApi.Subscriptions.CreateEventSubSubscriptionAsync(_userDetails.UserId, _userDetails.AccessToken, _eventSub.SessionId, SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_REMOVE, CancellationToken.None);
                await _helixApi.Subscriptions.CreateEventSubSubscriptionAsync(_userDetails.UserId, _userDetails.AccessToken, _eventSub.SessionId, SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_REDEMPTION_ADD, CancellationToken.None);
            }
        }

        private void OnStreamOnline(object? sender, StreamOnlineArgs e)
        {
            _logger.LogInformation("{user} has gone online at {timeStamp}.", e.Notification.Payload.Event.BroadcasterUserName, e.Notification.Payload.Event.StartedAt);
        }

        private void OnStreamOffline(object? sender, StreamOfflineArgs e)
        {
            _logger.LogInformation("{user} has gone offline at {timeStamp}.", e.Notification.Payload.Event.BroadcasterUserName, DateTime.Now);
        }

        private void OnErrorMessage(object? sender, ErrorMessageArgs e)
        {
            _logger.LogError("EventSub ran into an error. Message {mesage}. Exception {exception}.", e.Message, e.Exception);
        }

        private void OnClientDisconnected(object? sender, ClientDisconnectedArgs e)
        {
            _logger.LogWarning("EventSub client disconnected. Status {status}. Reason {reason}.", e.WebSocketCloseStatus, e.CloseStatusDescription);
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

        private void OnCustomRewardAdd(object? sender, CustomRewardAddArgs e)
        {
            var customReward = e.Notification.Payload.Event;
            _logger.LogInformation("Received custom reward add notification. Reward: {id} - {name}", customReward.Id, customReward.Title);

            using var scope = _serviceScopeFactory.CreateScope();
            var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            SaveReward(customReward, uberRepository);
        }

        private void OnCustomRewardUpdate(object? sender, CustomRewardUpdateArgs e)
        {
            var customReward = e.Notification.Payload.Event;
            _logger.LogInformation("Received custom reward update notification. Reward: {id} - {name}", customReward.Id, customReward.Title);

            using var scope = _serviceScopeFactory.CreateScope();
            var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            SaveReward(customReward, uberRepository);
        }

        private static void SaveReward(CustomReward customReward, IUberRepository uberRepository)
        {
            // var imageUrl = customReward.Image is null ? customReward.DefaultImage.Url1x : customReward.Image.Url1x;
            var rewards = uberRepository.Find<Reward>(r => r.Id.Equals(customReward.Id));
            var reward = rewards.FirstOrDefault();

            if (reward is not null)
            {
                // reward.ImageUrl = imageUrl;
                reward.Title = customReward.Title;
                reward.Prompt = customReward.Prompt;
                reward.BackgroundColor = customReward.BackgroundColor;
                reward.Speech = new Speech(customReward.IsUserInputRequired, reward.Speech.VoiceIndex);
            }
            else
            {
                reward = new Reward
                {
                    Id = customReward.Id,
                    // ImageUrl = imageUrl,
                    Title = customReward.Title,
                    Prompt = customReward.Prompt,
                    StreamerId = customReward.BroadcasterUserId,
                    BackgroundColor = customReward.BackgroundColor,
                    Speech = new Speech(customReward.IsUserInputRequired)
                };
            }

            uberRepository.Save(reward);
        }

        private void OnCustomRewardRemove(object? sender, CustomRewardRemoveArgs e)
        {
            var customReward = e.Notification.Payload.Event;
            _logger.LogInformation("Received custom reward delete notification. Reward: {id} - {name}", customReward.Id, customReward.Title);

            using var scope = _serviceScopeFactory.CreateScope();
            var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            var rewards = uberRepository.Find<Reward>(r => r.Id.Equals(customReward.Id));
            var reward = rewards.FirstOrDefault();

            if (reward is not null)
                uberRepository.Delete(reward);
        }

        private async void OnChannelPointsCustomRewardRedemption(object? sender, CustomRewardRedemptionArgs e)
        {
            var redemption = e.Notification.Payload.Event;
            _logger.LogInformation("On {streamer}'s channel: {user} redeemed {title} at {redeemedAt}.",
                redemption.BroadcasterUserName, redemption.UserName, redemption.Reward.Title, redemption.RedeemedAt);

            using var scope = _serviceScopeFactory.CreateScope();
            var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            var rewards = uberRepository.Find<Reward>(r => r.Id.Equals(redemption.Reward.Id));

            if (!rewards.Any())
                return;

            var reward = rewards.First();

            if (reward.Speech.Enabled)
            {
                var textToSpeechEvent = new
                {
                    reward.Speech.VoiceIndex,
                    Message = redemption.UserInput
                };

                await _hubContext.Clients.All.SendAsync(TEXT_TO_SPEECH_EVENT, textToSpeechEvent);
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
                await _hubContext.Clients.All.SendAsync(AUDIO_EVENT, data);
            else
                await _hubContext.Clients.All.SendAsync(VIDEO_EVENT, data);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var uberRepository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
                var users = uberRepository.FindAll<User>();

                while (!users.Any())
                {
                    await Task.Delay(1000, cancellationToken);
                    users = uberRepository.FindAll<User>();
                }

                if (users.Any())
                {
                    var user = users.First();
                    _logger.LogInformation("Initializing event sub connection. User: {id} - {name}", user.Id, user.Name);

                    var tokenRefreshPolicy = userService.CreateTokenRefreshPolicy(user.Id);
                    var helixSubscription = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                        await _helixApi.Subscriptions.GetEventSubSubscriptionAsync(user.Id, tokenRefreshPolicy.AccessToken, cancellationToken), tokenRefreshPolicy.ContextData);
                    var inactiveSubscriptions = helixSubscription.Data.Where(x => _inactiveSubscriptionStatus.Contains(x.SubscriptionStatus)).ToList();

                    foreach (var subscription in inactiveSubscriptions)
                        await DeleteSubscription(user.Id, tokenRefreshPolicy.AccessToken, subscription.Id, cancellationToken);

                    var twitchUsers = await _helixApi.Users.GetUsersAsync(Array.Empty<string>(), tokenRefreshPolicy.AccessToken, cancellationToken);

                    if (twitchUsers.Any())
                    {
                        var twitchUser = twitchUsers.First();
                        _logger.LogInformation("Verifying user broadcaster type. User: {id} - {name} - {type}.", twitchUser.Id, twitchUser.Login, twitchUser.UserBroadcasterType);

                        if (twitchUser.UserBroadcasterType is not BroadcasterType.NORMAL)
                        {
                            _userDetails.UserId = user.Id;
                            _userDetails.AccessToken = tokenRefreshPolicy.AccessToken;
                            await _eventSub.ConnectAsync();
                        }
                    }
                }

            }, cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _eventSub.DisconnectAsync();
        }

        private async Task DeleteSubscription(string userId, string accessToken, string subscriptionId, CancellationToken cancellationToken)
        {
            await _helixApi.Subscriptions.DeleteEventSubSubscriptionAsync(userId, accessToken, subscriptionId, cancellationToken);
        }
    }

    internal class UserDetails
    {
        public string UserId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
    }
}
