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
        private const string ID = "Id";

        private readonly HelixApi _helixApi;
        private readonly IUserManager _userManager;
        private readonly IAssetFileService _assetFileService;
        private readonly IRepository<Entities.Reward> _repository;
        private readonly ILogger<RewardService> _logger;

        public RewardService(HelixApi helixApi,
                             IUserManager userManager,
                             IRepository<Entities.Reward> repository,
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
            var claim = userPrincipal.Claims.First(c => c.Type.Equals(ID));

            var rewards = await _repository.FindAsync(r => r.StreamerId.Equals(claim.Value), context.CancellationToken);

            if (rewards.Count == 0)
            {
                _logger.LogInformation("No rewards found. Searching external server.");
                rewards = await SynchronizeRewardsAsync(claim.Value, context.CancellationToken);
            }

            foreach (var reward in rewards)
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
            reward = await _repository.UpdateAsync(reward, context.CancellationToken);

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
            var claim = userPrincipal.Claims.First(c => c.Type.Equals(ID));

            Entities.Reward? reward = null;

            // TODO: This method needs review. Keep it simple for now but:
            // 1. Consider batching for large streams 
            // 2. Process every single item and rollback the transaction if validation fail
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                var request = requestStream.Current;

                var isGuid = Guid.TryParse(request.RewardId, out var rewardId);

                if (!isGuid || rewardId == Guid.Empty)
                    throw new ArgumentException($"Invalid Reward Id: {rewardId}.", nameof(request.RewardId));

                reward = await FetchRewardAsync(rewardId, context.CancellationToken);
                reward.AddAsset(FileName.FromString(request.FileName), request.Volume);
                await _assetFileService.AddAssetFileAsync(claim.Value, reward.Title, FileName.FromString(request.FileName), request.File, context.CancellationToken);
                await _repository.UpdateAsync(reward, context.CancellationToken);
            }

            return new RewardResponse
            {
                Reward = RewardProto.FromEntity(reward!)
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
            await _repository.UpdateAsync(reward, context.CancellationToken);
            reward.AddAsset(FileName.FromString(request.FileName), request.Volume);
            reward = await _repository.UpdateAsync(reward, context.CancellationToken);

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
            var claim = userPrincipal.Claims.First(c => c.Type.Equals(ID));

            var rewardIdExists = Guid.TryParse(request.RewardId, out var rewardId);

            if (!rewardIdExists || rewardId == Guid.Empty)
                throw new ArgumentException($"Invalid Reward Id: {request.RewardId}.", nameof(request.RewardId));

            var reward = await FetchRewardAsync(rewardId, context.CancellationToken);

            foreach (var fileName in request.FileName)
            {
                _assetFileService.DeleteAssetFile(claim.Value, reward.Title, FileName.FromString(fileName));
                reward.RemoveAsset(fileName.ToString());
            }

            reward = await _repository.UpdateAsync(reward, context.CancellationToken);

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
        /// <returns>A collection of rewards.</returns>
        private async Task<List<Entities.Reward>> SynchronizeRewardsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var rewards = new List<Entities.Reward>();
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

                    foreach (var entity in entities)
                    {
                        await _repository.AddAsync(entity, cancellationToken);
                    }

                    rewards.AddRange(entities);
                    return rewards;
                }

                _logger.LogInformation("No rewards were found.");
                return rewards;
            }

            _logger.LogInformation("Unable to find user in external server.");
            return rewards;
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
            return await _repository.FindByIdAsync(rewardId.ToString(), cancellationToken);
        }
        #endregion
    }
}
