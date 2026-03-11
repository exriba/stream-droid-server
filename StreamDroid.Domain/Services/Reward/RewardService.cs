using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SharpTwitch.Core.Enums;
using SharpTwitch.Helix;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.Interfaces;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Services.AssetFile;
using StreamDroid.Domain.Services.User;
using System.Security.Claims;
using static GrpcRewardService;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Reward
{
    /// <summary>
    /// Reward Service API.
    /// </summary>
    [Authorize]
    public sealed class RewardService : GrpcRewardServiceBase
    {
        private readonly HelixApi _helixApi;
        private readonly IUserManager _userManager;
        private readonly IAssetFileService _assetFileService;
        private readonly IUberRepository _repository;
        private readonly ILogger<RewardService> _logger;

        public RewardService(HelixApi helixApi,
                             IUserManager userManager,
                             IUberRepository repository,
                             IAssetFileService assetFileService,
                             ILogger<RewardService> logger)
        {
            _helixApi = helixApi;
            _repository = repository;
            _userManager = userManager;
            _assetFileService = assetFileService;
            _logger = logger;
        }

        /// <summary>
        /// Finds a reward by the given id.
        /// </summary>
        /// <returns>A reward.</returns>
        /// <exception cref="ArgumentException">If the reward id is an invalid GUID</exception>
        public override async Task<RewardResponse> FindReward(RewardRequest request, ServerCallContext context)
        {
            var rewardIdExists = Guid.TryParse(request.RewardId, out var rewardId);

            if (!rewardIdExists || rewardId == Guid.Empty)
                throw new ArgumentException($"Invalid Reward Id: {request.RewardId}.", nameof(request.RewardId));

            var reward = await FetchRewardAsync(rewardId, context.CancellationToken);

            return new RewardResponse
            {
                Reward = RewardProto.FromEntity(reward),
            };
        }

        /// <summary>
        /// Finds a collection of rewards for the current user id.
        /// </summary>
        /// <returns>A collection of rewards.</returns>
        public override async Task FindUserRewards(Empty request, IServerStreamWriter<RewardResponse> responseStream, ServerCallContext context)
        {
            var userPrincipal = context.GetHttpContext().User;
            var claim = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)!;

            var rewardCount = await _repository.CountAsync<Entities.Reward>(r => r.StreamerId.Equals(claim.Value), context.CancellationToken);

            if (rewardCount == 0)
            {
                _logger.LogInformation("No rewards found. Searching external server.");
                await SynchronizeRewardsAsync(claim.Value, context.CancellationToken);
            }

            await foreach (var reward in _repository.FindStreamAsync<Entities.Reward>(r => r.StreamerId.Equals(claim.Value), cancellationToken: context.CancellationToken))
            {
                var response = new RewardResponse
                {
                    Reward = RewardProto.FromEntity(reward)
                };

                await responseStream.WriteAsync(response, context.CancellationToken);
            }
        }

        /// <summary>
        /// Updates the speech for the given reward.
        /// </summary>
        /// <returns>A reward.</returns>
        /// <exception cref="ArgumentException">If the reward id is an invalid GUID</exception>
        public override async Task<RewardResponse> UpdateRewardSpeech(RewardSpeechRequest request, ServerCallContext context)
        {
            var rewardIdExists = Guid.TryParse(request.RewardId, out var rewardId);

            if (!rewardIdExists || rewardId == Guid.Empty)
                throw new ArgumentException($"Invalid Reward Id: {request.RewardId}.", nameof(request.RewardId));

            var reward = await FetchRewardAsync(rewardId, context.CancellationToken);
            reward.Speech = new Speech(enabled: request.Speech.Enabled, voiceIndex: request.Speech.VoiceIndex);
            _repository.Update(reward);
            await _repository.SaveChangesAsync(context.CancellationToken);

            return new RewardResponse
            {
                Reward = RewardProto.FromEntity(reward),
            };
        }

        /// <summary>
        /// Adds assets to the given reward.
        /// </summary>
        /// <exception cref="ArgumentException">If the reward id is an invalid GUID</exception>
        /// <returns>A reward.</returns>
        public override async Task<RewardResponse> AddRewardAssets(IAsyncStreamReader<AddRewardAssetRequest> requestStream, ServerCallContext context)
        {
            var userPrincipal = context.GetHttpContext().User;
            var claim = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)!;

            string? rewardId = null;
            Entities.Reward? reward = null;

            // TODO: Consider batching for large streams 
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                var request = requestStream.Current;

                if (!Guid.TryParse(request.RewardId, out var id) || id == Guid.Empty)
                    throw new ArgumentException($"Invalid Reward Id: {id}.");

                rewardId ??= request.RewardId;

                if (rewardId != request.RewardId)
                    throw new ArgumentException($"Invalid Reward Id: {request.RewardId}. All items must share the same reward id {rewardId}.");

                reward ??= await FetchRewardAsync(id, context.CancellationToken);
                var fileName = FileName.FromString(request.FileName);
                reward.AddAsset(fileName, request.Volume);
                await _assetFileService.AddAssetFileAsync(claim.Value, reward.Title, fileName, request.File);
            }

            if (reward is null)
                throw new InvalidOperationException("Stream cannot be empty.");

            _repository.Update(reward);
            await _repository.SaveChangesAsync(context.CancellationToken);

            return new RewardResponse
            {
                Reward = RewardProto.FromEntity(reward)
            };
        }

        /// <summary>
        /// Updates assets from the given reward.
        /// </summary>
        /// <exception cref="ArgumentException">If the reward id is an invalid GUID</exception>
        /// <returns>A reward.</returns>
        public override async Task<RewardResponse> UpdateRewardAssets(UpdateRewardAssetRequest request, ServerCallContext context)
        {
            var rewardIdExists = Guid.TryParse(request.RewardId, out var rewardId);

            if (!rewardIdExists || rewardId == Guid.Empty)
                throw new ArgumentException($"Invalid Reward Id: {request.RewardId}.", nameof(request.RewardId));

            var reward = await FetchRewardAsync(rewardId, context.CancellationToken);
            reward.RemoveAsset(request.FileName);
            reward.AddAsset(FileName.FromString(request.FileName), request.Volume);
            _repository.Update(reward);
            await _repository.SaveChangesAsync(context.CancellationToken);

            return new RewardResponse
            {
                Reward = RewardProto.FromEntity(reward)
            };
        }

        /// <summary>
        /// Removes assets from the given reward.
        /// </summary>
        /// <exception cref="ArgumentException">If the reward id is an invalid GUID</exception>
        /// <returns>A reward.</returns>
        public override async Task<RewardResponse> RemoveRewardAssets(RemoveRewardAssetRequest request, ServerCallContext context)
        {
            var userPrincipal = context.GetHttpContext().User;
            var claim = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)!;

            var rewardIdExists = Guid.TryParse(request.RewardId, out var rewardId);

            if (!rewardIdExists || rewardId == Guid.Empty)
                throw new ArgumentException($"Invalid Reward Id: {request.RewardId}.", nameof(request.RewardId));

            var reward = await FetchRewardAsync(rewardId, context.CancellationToken);

            foreach (var fileName in request.FileName)
            {
                _assetFileService.DeleteAssetFile(claim.Value, reward.Title, FileName.FromString(fileName));
                reward.RemoveAsset(fileName.ToString());
            }

            _repository.Update(reward);
            await _repository.SaveChangesAsync(context.CancellationToken);

            return new RewardResponse
            {
                Reward = RewardProto.FromEntity(reward)
            };
        }

        /// <summary>
        /// Synchronizes external rewards for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="cancellationToken">cancellation token</param>
        private async Task SynchronizeRewardsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var tokenRefreshPolicy = await _userManager.CreateTokenRefreshPolicyAsync(userId, cancellationToken);

            var twitchUsers = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                await _helixApi.Users.GetUsersAsync([], tokenRefreshPolicy.AccessToken, cancellationToken), tokenRefreshPolicy.ContextData);

            if (twitchUsers.Any())
            {
                var twitchUser = twitchUsers.First();

                _logger.LogInformation("Found user with id {id} and name {name}.", twitchUser.Id, twitchUser.DisplayName);

                if (twitchUser.UserBroadcasterType is not BroadcasterType.NORMAL)
                {
                    var twitchRewards = await _helixApi.CustomRewards.GetCustomRewardsAsync(userId, tokenRefreshPolicy.AccessToken, cancellationToken);

                    if (twitchRewards.Any())
                        _logger.LogInformation("Importing rewards.");

                    var entities = twitchRewards.Select(customReward =>
                    {
                        var imageUrl = customReward.Image == null ? customReward.DefaultImage.Url1x : customReward.Image.Url1x;
                        return new Entities.Reward
                        {
                            Id = customReward.Id,
                            ImageUrl = imageUrl,
                            Title = customReward.Title,
                            Prompt = customReward.Prompt,
                            StreamerId = customReward.BroadcasterUserId,
                            BackgroundColor = customReward.BackgroundColor,
                            Speech = new Speech(customReward.IsUserInputRequired)
                        };
                    });

                    if (entities.Any())
                    {
                        foreach (var entity in entities)
                        {
                            await _repository.AddAsync(entity, cancellationToken);
                        }

                        await _repository.SaveChangesAsync(cancellationToken);
                    }

                    return;
                }

                _logger.LogInformation("No rewards were found.");
                return;
            }

            _logger.LogInformation("Unable to find user in external server.");
        }

        #region Helpers
        /// <summary>
        /// Finds a reward by the given id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>A reward entity</returns>
        /// <exception cref="EntityNotFoundException">If the reward is not found</exception>
        private async Task<Entities.Reward> FetchRewardAsync(Guid rewardId, CancellationToken cancellationToken = default)
        {
            return await FetchRewardByIdAsync(rewardId, cancellationToken) ?? throw new EntityNotFoundException(rewardId.ToString());
        }

        /// <summary>
        /// Finds a reward by the given id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>A reward entity.</returns>
        /// <exception cref="ArgumentException">If the reward id is an empty GUID</exception>
        private async Task<Entities.Reward?> FetchRewardByIdAsync(Guid rewardId, CancellationToken cancellationToken = default)
        {
            if (rewardId == Guid.Empty)
                throw new ArgumentException("Invalid Reward Id.", nameof(rewardId));
            return await _repository.FindByIdAsync<Entities.Reward>(rewardId.ToString(), cancellationToken);
        }
        #endregion
    }
}
