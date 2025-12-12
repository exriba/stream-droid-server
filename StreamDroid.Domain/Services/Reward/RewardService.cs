using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SharpTwitch.Core.Enums;
using SharpTwitch.Helix;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Services.AssetFile;
using StreamDroid.Domain.Services.User;
using StreamDroid.Infrastructure.Persistence;
using static GrpcRewardService;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Reward
{
    /// <summary>
    /// Service class responsible for handling all Reward related logic.
    /// </summary>
    [Authorize]
    public sealed class RewardService : GrpcRewardServiceBase
    {
        private const string ID = "Id";

        private readonly HelixApi _helixApi;
        private readonly IUserService _userService;
        private readonly IAssetFileService _assetFileService;
        private readonly IRepository<Entities.Reward> _repository;
        private readonly ILogger<RewardService> _logger;

        public RewardService(HelixApi helixApi,
                             IUserService userService,
                             IRepository<Entities.Reward> repository,
                             IAssetFileService assetFileService,
                             ILogger<RewardService> logger)
        {
            _helixApi = helixApi;
            _repository = repository;
            _userService = userService;
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

            var reward = await FetchRewardAsync(rewardId);

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

            var rewards = await _repository.FindAsync(r => r.StreamerId.Equals(claim.Value));

            if (rewards.Count == 0)
            {
                _logger.LogInformation("No rewards found. Searching external server.");
                rewards = await SynchronizeRewardsAsync(claim.Value);
            }

            foreach (var reward in rewards)
            {
                var response = new RewardResponse
                {
                    Reward = RewardProto.FromEntity(reward)
                };
                await responseStream.WriteAsync(response);
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

            var reward = await FetchRewardAsync(rewardId);
            reward.Speech = new Speech(enabled: request.Speech.Enabled, voiceIndex: request.Speech.VoiceIndex);
            reward = await _repository.UpdateAsync(reward);

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

            var id = Guid.Empty;
            var tasks = new List<Task>(3);

            while (await requestStream.MoveNext())
            {
                if (tasks.Count == 3)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                var request = requestStream.Current;

                if (id == Guid.Empty)
                {
                    var isGuid = Guid.TryParse(request.RewardId, out var rewardId);

                    if (!isGuid || rewardId == Guid.Empty)
                        throw new ArgumentException($"Invalid Reward Id: {rewardId}.", nameof(request.RewardId));

                    id = rewardId;
                }

                if (request.RewardId != id.ToString())
                    throw new ArgumentException("All requests must have the same reward id.");


                var innerReward = await FetchRewardAsync(id);
                innerReward.AddAsset(FileName.FromString(request.FileName), request.Volume);
                var task = _assetFileService.AddAssetFilesAsync(claim.Value, innerReward.Title, FileName.FromString(request.FileName), request.File);
                tasks.Add(task);
            }

            var reward = await FetchRewardAsync(id);
            await Task.WhenAll(tasks);
            await _repository.UpdateAsync(reward);
            tasks.Clear();

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

            var reward = await FetchRewardAsync(rewardId);
            reward.RemoveAsset(request.FileName);
            await _repository.UpdateAsync(reward);
            reward.AddAsset(FileName.FromString(request.FileName), request.Volume);
            reward = await _repository.UpdateAsync(reward);

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

            var reward = await FetchRewardAsync(rewardId);

            foreach (var fileName in request.FileName)
            {
                _assetFileService.DeleteAssetFile(claim.Value, reward.Title, FileName.FromString(fileName));
                reward.RemoveAsset(fileName.ToString());
            }

            reward = await _repository.UpdateAsync(reward);

            return new RewardResponse
            {
                Reward = RewardProto.FromEntity(reward)
            };
        }

        /// <summary>
        /// Synchronizes external rewards for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>A collection of rewards.</returns>
        private async Task<List<Entities.Reward>> SynchronizeRewardsAsync(string userId)
        {
            var rewards = new List<Entities.Reward>();
            var tokenRefreshPolicy = await _userService.CreateTokenRefreshPolicyAsync(userId);

            var twitchUsers = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                await _helixApi.Users.GetUsersAsync([], tokenRefreshPolicy.AccessToken, CancellationToken.None), tokenRefreshPolicy.ContextData);

            if (twitchUsers.Any())
            {
                var twitchUser = twitchUsers.First();
                _logger.LogInformation("Found user with id {id} and name {name}.", twitchUser.Id, twitchUser.DisplayName);

                if (twitchUser.UserBroadcasterType is not BroadcasterType.NORMAL)
                {
                    var twitchRewards = await _helixApi.CustomRewards.GetCustomRewardsAsync(userId, tokenRefreshPolicy.AccessToken, CancellationToken.None);

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
                        await _repository.AddAsync(entity);
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
        /// <returns>A reward entity</returns>
        /// <exception cref="EntityNotFoundException">If the reward is not found</exception>
        private async Task<Entities.Reward> FetchRewardAsync(Guid rewardId)
        {
            return await FetchRewardByIdAsync(rewardId) ?? throw new EntityNotFoundException(rewardId.ToString());
        }

        /// <summary>
        /// Finds a reward by the given id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <returns>A reward entity.</returns>
        /// <exception cref="ArgumentException">If the reward id is an empty GUID</exception>
        private async Task<Entities.Reward?> FetchRewardByIdAsync(Guid rewardId)
        {
            if (rewardId == Guid.Empty)
                throw new ArgumentException("Invalid Reward Id.", nameof(rewardId));
            return await _repository.FindByIdAsync(rewardId.ToString());
        }
        #endregion
    }
}
