using Grpc.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Exceptions;
using SharpTwitch.EventSub;
using SharpTwitch.EventSub.Core.EventArgs;
using SharpTwitch.EventSub.Core.EventArgs.Channel.Redemption;
using SharpTwitch.EventSub.Core.EventArgs.Channel.Reward;
using SharpTwitch.EventSub.Core.EventArgs.Stream;
using SharpTwitch.EventSub.Core.EventMessageArgs;
using SharpTwitch.Helix;
using SharpTwitch.Helix.Models.Channel.Reward;
using StreamDroid.Core.Enums;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Settings;
using Entities = StreamDroid.Core.Entities;
using EventType = Grpc.Model.NotificationEvent.Types.EventType;
using Speech = StreamDroid.Core.ValueObjects.Speech;

// TODO:
// 1. Review these restrictions https://dev.twitch.tv/docs/eventsub/manage-subscriptions/#subscription-limits (Future updates)
namespace StreamDroid.Domain.Services.Stream
{
    /// <summary>
    /// A background service designed to receive and handle Twitch EventSub messages. This class should run as it's own independent service, 
    /// decoupled from the web host for scalability and maintenance purposes.
    /// </summary>
    internal class TwitchEventSub : BackgroundService, ITwitchSubscriber
    {
        private static readonly IReadOnlySet<SubscriptionType> SUBSCRIPTION_TYPES = new HashSet<SubscriptionType>
        {
            { SubscriptionType.STREAM_ONLINE },
            { SubscriptionType.STREAM_OFFLINE },
            { SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_ADD },
            { SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_UPDATE },
            { SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_REMOVE },
            { SubscriptionType.CHANNEL_CHANNEL_POINTS_CUSTOM_REWARD_REDEMPTION_ADD },
        };

        private readonly EventSub _eventSub;
        private readonly IAppSettings _appSettings;
        private readonly ILogger<TwitchEventSub> _logger;
        private readonly ISet<string> _activeSubscribers;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly NotificationService _notificationService;

        public TwitchEventSub(EventSub eventSub,
                              IAppSettings appSettings,
                              IServiceScopeFactory serviceScopeFactory,
                              NotificationService notificationService,
                              ILogger<TwitchEventSub> logger)
        {
            _logger = logger;
            _eventSub = eventSub;
            _appSettings = appSettings;
            _notificationService = notificationService;
            _serviceScopeFactory = serviceScopeFactory;
            _activeSubscribers = new HashSet<string>();
        }

        /// <summary>
        /// Initializes Event Handlers and clears all active eventsub subscriptions, then disconnects and disposes client when the application shuts down. 
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initiating TwitchEventSub service.");

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

            _logger.LogInformation("Loaded event handlers.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Shutting down TwitchEventSub.");

                    await DeleteSubscriptionsAsync(CancellationToken.None);

                    if (_eventSub.WebSocketClient!.Connected)
                        await _eventSub.DisconnectAsync(CancellationToken.None);

                    var sessionId = _eventSub.SessionId == string.Empty ? "N/A" : _eventSub.SessionId;
                    _logger.LogInformation("Disposing event sub with session {id}.", sessionId);
                    await _eventSub.DisposeAsync();
                }
            }
        }

        /// <inheritdoc/>
        public async Task SubscribeAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (_activeSubscribers.Contains(userId))
                return;

            await ConnectAsync(); // Might need to lock this to avoid race conditions

            await CreateSubscriptionsAsync(userId, cancellationToken);
        }

        private async Task ConnectAsync()
        {
            if (!_eventSub.WebSocketClient.Connected)
            {
                await _eventSub.ConnectAsync(null, CancellationToken.None);
                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                try
                {
                    while (!_eventSub.WebSocketClient.Connected)
                    {
                        await Task.Delay(500, source.Token);
                    }
                }
                catch (OperationCanceledException ex) when (source.IsCancellationRequested)
                {
                    throw new TimeoutException("Unable to establish connection Twitch EventSub.", ex);
                }
            }
        }

        private async Task CreateSubscriptionsAsync(string userId, CancellationToken cancellationToken = default)
        {
            _activeSubscribers.Add(userId);

            _logger.LogInformation("Creating subscriptions for user id {id}.", userId);

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var helixApi = scope.ServiceProvider.GetRequiredService<HelixApi>();
                var userManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
                var tokenRefreshPolicy = await userManager.CreateTokenRefreshPolicyAsync(userId, CancellationToken.None);

                try
                {
                    var helixSubscriptionResponse = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                        await helixApi.Subscriptions.GetEventSubSubscriptionAsync(userId, tokenRefreshPolicy.AccessToken, cancellationToken), tokenRefreshPolicy.ContextData);

                    var tasks = SUBSCRIPTION_TYPES.Select(x =>
                    {
                        _logger.LogInformation("Session {session}: Creating subscription {type} on {date}.", _eventSub.SessionId, x, DateTime.UtcNow);
                        return helixApi.Subscriptions.CreateEventSubSubscriptionAsync(userId, tokenRefreshPolicy.AccessToken, _eventSub.SessionId, x, cancellationToken);
                    });

                    await Task.WhenAll(tasks);
                }
                catch (UnauthorizedRequestException ex)
                {
                    _logger.LogError(ex, "Unauthorized access request from user {userId}.", userId);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Unable to connect to the remote server.");
                }
            }
        }

        #region EventSub Handlers

        private void OnClientConnected(object? sender, ClientConnectedArgs e)
        {
            _logger.LogInformation("EventSub client connected");
        }

        private async void OnClientDisconnected(object? sender, ClientDisconnectedArgs e)
        {
            var reason = string.IsNullOrWhiteSpace(e.CloseStatusDescription) ? "N/A" : e.CloseStatusDescription;
            _logger.LogWarning("EventSub client disconnected. Status {status}. Reason {reason}.", e.WebSocketCloseStatus, reason);


            // Need to implement resilience policy (Polly) for some of these
            //    1000 => "Stay closed (Normal)",
            //    1001 => "Reconnect (Server going away)",
            //    1012 => "Reconnect with Jitter (Service Restart)",
            //    1013 => "Reconnect with Backoff (Server Busy)",
            //    1006 => "Reconnect Immediate (Abnormal)",
            int? statusCode = (int?)e.WebSocketCloseStatus;
            if (statusCode.HasValue && statusCode.Value == 1006)
                await _eventSub.ReconnectAsync();
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
            var imageUrl = customReward.Image is null ? customReward.DefaultImage.Url1x : customReward.Image.Url1x;

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            var reward = await repository.FindByIdAsync<Entities.Reward>(customReward.Id);

            if (reward is not null)
            {
                reward.ImageUrl = imageUrl;
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
                    ImageUrl = imageUrl,
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
            var repository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            await repository.DeleteAsync<Entities.Reward>(customReward.Id);
        }

        private async void OnChannelPointsCustomRewardRedemption(object? sender, CustomRewardRedemptionArgs e)
        {
            var redeem = e.Notification.Payload.Event;
            _logger.LogInformation("Streamer {streamer}: {user} redeemed {title} at {redeemedAt}.",
                redeem.BroadcasterUserName, redeem.UserName, redeem.Reward.Title, redeem.RedeemedAt);

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUberRepository>();
            var reward = await repository.FindByIdAsync<Entities.Reward>(redeem.Reward.Id);

            if (reward is null)
            {
                scope.Dispose();
                return;
            }

            var redemption = new Entities.Redemption
            {
                Id = Guid.NewGuid().ToString(),
                UserId = redeem.UserId,
                UserName = redeem.UserName,
                Reward = reward
            };

            await repository.AddAsync(redemption);
            scope.Dispose();

            if (reward.Speech.Enabled)
            {
                var textToSpeechEvent = new NotificationEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = EventType.Speech,
                    StreamerId = redeem.BroadcasterUserId,
                    TextToSpeechEvent = new TextToSpeechEvent
                    {
                        Message = redeem.UserInput,
                        VoiceIndex = reward.Speech.VoiceIndex
                    }
                };

                await _notificationService.PublishNotificationAsync(textToSpeechEvent);
            }

            if (!reward.TryGetRandomAsset(out var asset))
                return;

            var uriString = string.Join("/", _appSettings.ServerUri, _appSettings.StaticAssetPath, redeem.BroadcasterUserId, reward.Title, asset!.ToString());

            var assetFileEvent = new NotificationEvent
            {
                Id = Guid.NewGuid().ToString(),
                EventType = asset!.FileName.MediaExtension == MediaExtension.MP3 ? EventType.Audio : EventType.Video,
                StreamerId = redeem.BroadcasterUserId,
                AssetFileEvent = new AssetFileEvent
                {
                    Volume = asset.Volume,
                    Uri = uriString
                }
            };

            await _notificationService.PublishNotificationAsync(assetFileEvent);
        }

        // TODO: Need to alert user/admin.
        private void OnErrorMessage(object? sender, ErrorMessageArgs e)
        {
            var message = string.IsNullOrWhiteSpace(e.Message) ? "N/A" : e.Message;
            _logger.LogError(e.Exception, "EventSub ran into an error.\n{mesage}.", message);
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

        #endregion

        /// <inheritdoc/>
        public async Task UnsubscribeAsync(string userId, CancellationToken cancellationToken = default)
        {
            await DeleteSubscriptionsAsync(userId, cancellationToken);
        }

        private async Task DeleteSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cleaning up subscriptions.");

            foreach (var userId in _activeSubscribers)
                await DeleteSubscriptionsAsync(userId, cancellationToken);
        }

        private async Task DeleteSubscriptionsAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (!_activeSubscribers.Contains(userId))
                return;

            _logger.LogInformation("Cleaning up subscriptions for user id {id}.", userId);

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var helixApi = scope.ServiceProvider.GetRequiredService<HelixApi>();
                var userManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
                var tokenRefreshPolicy = await userManager.CreateTokenRefreshPolicyAsync(userId);

                try
                {
                    var helixSubscriptionResponse = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                         await helixApi.Subscriptions.GetEventSubSubscriptionAsync(userId, tokenRefreshPolicy.AccessToken, cancellationToken), tokenRefreshPolicy.ContextData);

                    var tasks = helixSubscriptionResponse.Data.Select(x =>
                    {
                        _logger.LogInformation("Session {session}: Deleting subscription {type} with id {id} which was created on {date}.", x.Transport.SessionId, x.Type, x.Id, x.CreatedAt);
                        return helixApi.Subscriptions.DeleteEventSubSubscriptionAsync(x.Condition.BroadcasterUserId, tokenRefreshPolicy.AccessToken, x.Id, cancellationToken);
                    });

                    await Task.WhenAll(tasks);
                }
                catch (UnauthorizedRequestException ex)
                {
                    _logger.LogError(ex, "Unauthorized access request from user {userId}.", userId);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Unable to connect to the remote server.");
                }
            }

            _activeSubscribers.Remove(userId);
        }
    }
}
