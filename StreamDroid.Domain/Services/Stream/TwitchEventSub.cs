using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
using StreamDroid.Domain.Services.Stream.Events;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure.Persistence;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using Entities = StreamDroid.Core.Entities;

// TODO:
// 1. Review these restrictions https://dev.twitch.tv/docs/eventsub/manage-subscriptions/#subscription-limits (Future updates)
namespace StreamDroid.Domain.Services.Stream
{
    /// <summary>
    /// Default implementation of <see cref="ITwitchEventSub"/>.
    /// </summary>
    internal class TwitchEventSub : IHostedService, ITwitchEventSub
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
        private volatile bool NetworkIsAvailable = false;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDictionary<string, Func<EventBase, Task>?> _usersSubscribed;

        public TwitchEventSub(EventSub eventSub,
                              IAppSettings appSettings,
                              IServiceScopeFactory serviceScopeFactory,
                              ILogger<TwitchEventSub> logger)
        {
            _logger = logger;
            _eventSub = eventSub;
            _appSettings = appSettings;
            _serviceScopeFactory = serviceScopeFactory;
            _usersSubscribed = new Dictionary<string, Func<EventBase, Task>?>();

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

        private void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
            => NetworkIsAvailable = e.IsAvailable;

        ///<Summary>
        /// Initializes event sub client with twitch.
        /// Includes workaround for <see cref="TwitchEventSub.StopAsync(CancellationToken)">StopAsync</see>
        ///</Summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            NetworkIsAvailable = NetworkInterface.GetIsNetworkAvailable();
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (NetworkIsAvailable)
                {
                    await ClearSubscriptionsAsync();

                    if (_usersSubscribed.Keys.Any())
                        await _eventSub.ConnectAsync();

                    break;
                }

                await Task.Delay(1000);
            }
        }

        /// <inheritdoc/>
        public async Task SubscribeAsync(string userId, Func<EventBase, Task> notificationHandler)
        {
            if (_usersSubscribed.ContainsKey(userId))
            {
                _usersSubscribed[userId] = notificationHandler;
                return;
            }

            if (_eventSub.webSocketClient.Connected)
                await SubscribeAsync(userId);
            else
                await _eventSub.ConnectAsync();

            _usersSubscribed.Add(userId, notificationHandler);
        }

        /// <inheritdoc/>
        public void UnsubscribeAsync(string userId)
        {
            if (!_usersSubscribed.ContainsKey(userId))
                return;

            _usersSubscribed[userId] = null;
        }

        private async void OnClientConnected(object? sender, ClientConnectedArgs e)
        {
            _logger.LogInformation("EventSub client connected");

            if (!e.ReconnectionRequested)
            {
                var userIds = _usersSubscribed.Keys.ToArray();
                await SubscribeAsync(userIds);
            }
        }

        private async Task SubscribeAsync(params string[] userIds)
        {
            if (_eventSub.SessionId != string.Empty)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var helixApi = scope.ServiceProvider.GetRequiredService<HelixApi>();
                    using var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                    foreach (var userId in userIds)
                    {
                        var tokenRefreshPolicy = await userService.CreateTokenRefreshPolicyAsync(userId);

                        try
                        {
                            var helixSubscriptionResponse = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                                await helixApi.Subscriptions.GetEventSubSubscriptionAsync(userId, tokenRefreshPolicy.AccessToken, CancellationToken.None), tokenRefreshPolicy.ContextData);

                            var tasks = SUBSCRIPTION_TYPES.Select(x =>
                            {
                                _logger.LogInformation("Session {session}: Creating subscription {type} on {date}.", _eventSub.SessionId, x, DateTime.UtcNow);
                                return helixApi.Subscriptions.CreateEventSubSubscriptionAsync(userId, tokenRefreshPolicy.AccessToken, _eventSub.SessionId, x, CancellationToken.None);
                            });

                            await Task.WhenAll(tasks);
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogError("Unable to connect to the remote server. Exception {ex}", ex);
                        }
                        
                    }
                }
            }
        }

        private async void OnClientDisconnected(object? sender, ClientDisconnectedArgs e)
        {
            var reason = string.IsNullOrWhiteSpace(e.CloseStatusDescription) ? "N/A" : e.CloseStatusDescription;
            _logger.LogWarning("EventSub client disconnected. Status {status}. Reason {reason}.", e.WebSocketCloseStatus, reason);

            if (e.WebSocketCloseStatus is WebSocketCloseStatus.Empty)
                await _eventSub.ReconnectAsync();
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

        // TODO:
        // 1. Need to alert user/admin.
        private void OnErrorMessage(object? sender, ErrorMessageArgs e)
        {
            var message = string.IsNullOrWhiteSpace(e.Message) ? "N/A" : e.Message;
            _logger.LogError("EventSub ran into an error. Message: {mesage} Exception: {exception}.", message, e.Exception);
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
            using var repository = scope.ServiceProvider.GetRequiredService<IRepository<Entities.Reward>>();
            var reward = await repository.FindByIdAsync(customReward.Id);

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
            using var repository = scope.ServiceProvider.GetRequiredService<IRepository<Entities.Reward>>();
            await repository.DeleteAsync(customReward.Id);
        }

        private async void OnChannelPointsCustomRewardRedemption(object? sender, CustomRewardRedemptionArgs e)
        {
            var redeem = e.Notification.Payload.Event;
            _logger.LogInformation("Streamer {streamer}: {user} redeemed {title} at {redeemedAt}.",
                redeem.BroadcasterUserName, redeem.UserName, redeem.Reward.Title, redeem.RedeemedAt);

            Entities.Reward? reward;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                using var rewardRepository = scope.ServiceProvider.GetRequiredService<IRepository<Entities.Reward>>();
                using var redemptionRepository = scope.ServiceProvider.GetRequiredService<IRedemptionRepository>();
                reward = await rewardRepository.FindByIdAsync(redeem.Reward.Id);

                if (reward is not null)
                {
                    var redemption = new Entities.Redemption
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = redeem.UserId,
                        UserName = redeem.UserName,
                        Reward = reward
                    };

                    await redemptionRepository.AddAsync(redemption);
                }
            }

            _usersSubscribed.TryGetValue(redeem.BroadcasterUserId, out var handler);

            if (reward is null || handler is null)
                return;

            if (reward.Speech.Enabled)
            {
                var textToSpeechEvent = new SpeechEvent
                {
                    Message = redeem.UserInput,
                    VoiceIndex = reward.Speech.VoiceIndex
                };

                await handler(textToSpeechEvent);
            }

            if (!reward.TryGetRandomAsset(out var asset))
                return;

            var eventType = asset!.FileName.MediaExtension == MediaExtension.MP3 ? EventType.AUDIO : EventType.VIDEO;
            var uriString = string.Join("/", _appSettings.ServerUri, _appSettings.StaticAssetPath, redeem.BroadcasterUserId, reward.Title, asset.ToString());

            var assetEvent = new AssetEvent(eventType)
            {
                Volume = asset.Volume,
                Uri = new Uri(uriString)
            };

            await handler(assetEvent);
        }

        /// <summary>
        /// Clears all active eventsub subscriptions, then disconnects and disposes client. 
        /// Due to the following open <see href="https://github.com/dotnet/runtime/issues/83093">issue</see>, Windows Services do not exit/shutdown
        /// gracefully and thus this method is never called. As a result, StartAsync will handle clean up by removing inactive subscriptions from previous sessions. 
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Task</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await ClearSubscriptionsAsync();
            _usersSubscribed.Clear();

            _logger.LogInformation("Disconnecting event sub with session {id}.", _eventSub.SessionId);
            await _eventSub.DisconnectAsync();

            _logger.LogInformation("Disposing event sub with session {id}.", _eventSub.SessionId);
            await _eventSub.DisposeAsync();
        }

        private async Task ClearSubscriptionsAsync()
        {
            _logger.LogInformation("Cleaning up unecessary subscriptions.");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var helixApi = scope.ServiceProvider.GetRequiredService<HelixApi>();
                using var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                
                if (!_usersSubscribed.Keys.Any())
                {
                    var users = await userService.FindUsersAsync();

                    foreach (var user in users)
                        _usersSubscribed.Add(user.Id, null);
                }

                foreach (var userId in _usersSubscribed.Keys)
                {
                    var tokenRefreshPolicy = await userService.CreateTokenRefreshPolicyAsync(userId);

                    try
                    {
                        var helixSubscriptionResponse = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                            await helixApi.Subscriptions.GetEventSubSubscriptionAsync(userId, tokenRefreshPolicy.AccessToken, CancellationToken.None), tokenRefreshPolicy.ContextData);

                        var tasks = helixSubscriptionResponse.Data.Select(x =>
                        {
                            _logger.LogInformation("Session {session}: Deleting subscription {type} with id {id} that was created on {date}.", x.Transport.SessionId, x.Type, x.Id, x.CreatedAt);
                            return helixApi.Subscriptions.DeleteEventSubSubscriptionAsync(x.Condition.BroadcasterUserId, tokenRefreshPolicy.AccessToken, x.Id, CancellationToken.None);
                        });

                        await Task.WhenAll(tasks);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError("Unable to connect to the remote server. Exception {ex}", ex);
                    }
                }
            }
        }
    }
}
