using Ardalis.GuardClauses;
using Mapster;
using SharpTwitch.Core.Enums;
using SharpTwitch.Helix;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Services.User;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Reward
{
    public sealed class RewardService : IRewardService
    {
        private readonly HelixApi _helixApi;
        private readonly IUserService _userService;
        private readonly IRepository<Entities.Reward> _repository;

        public RewardService(HelixApi helixApi, 
                             IUserService userService, 
                             IRepository<Entities.Reward> repository)
        {
            _helixApi = helixApi;
            _repository = repository;
            _userService = userService;
        }

        #region Rewards
        public async Task<RewardDto> FindRewardByIdAsync(Guid rewardId)
        {
            var reward = await FetchRewardAsync(rewardId);
            return RewardDto.FromEntity(reward);
        }

        public async Task<IReadOnlyCollection<RewardDto>> FindRewardsByStreamerIdAsync(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

            var entities = await _repository.FindAsync(r => r.StreamerId.Equals(userId));
            var rewards = entities.AsQueryable(); 
            return rewards.ProjectToType<RewardDto>().ToList();
        }

        public async Task UpdateRewardSpeechAsync(Guid rewardId, Speech speech)
        {
            var reward = await FetchRewardAsync(rewardId);
            reward.Speech = speech;
            await _repository.UpdateAsync(reward);
        }

        public async Task SynchronizeRewardsAsync(string userId)
        {
            var tokenRefreshPolicy = await _userService.CreateTokenRefreshPolicyAsync(userId);

            var twitchUsers = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                await _helixApi.Users.GetUsersAsync(Array.Empty<string>(), tokenRefreshPolicy.AccessToken, CancellationToken.None), tokenRefreshPolicy.ContextData);

            if (twitchUsers.Any())
            {
                var twitchUser = twitchUsers.First();

                if (twitchUser.UserBroadcasterType is not BroadcasterType.NORMAL)
                {
                    var twitchRewards = await _helixApi.CustomRewards.GetCustomRewardsAsync(userId, tokenRefreshPolicy.AccessToken, CancellationToken.None);

                    var rewards = twitchRewards.Select(customReward =>
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

                    foreach (var reward in rewards)
                    {
                        var rewardId = Guid.Parse(reward.Id);
                        var rewardEntity = await FetchRewardByIdAsync(rewardId);

                        if (rewardEntity is null)
                            await _repository.AddAsync(reward);
                        else
                        {
                            rewardEntity.Title = reward.Title;
                            rewardEntity.Prompt = reward.Prompt;
                            rewardEntity.ImageUrl = reward.ImageUrl;
                            rewardEntity.BackgroundColor = reward.BackgroundColor;
                            rewardEntity.Speech = new Speech(reward.Speech.Enabled, rewardEntity.Speech.VoiceIndex);
                            await _repository.UpdateAsync(rewardEntity);
                        }
                    }
                }
            }
        }
        #endregion

        #region Assets
        public async Task<IReadOnlyCollection<Asset>> FindAssetsByRewardIdAsync(Guid rewardId)
        {
            var reward = await FetchRewardAsync(rewardId);
            return reward.Assets;
        }

        public async Task<Tuple<string, IReadOnlyCollection<Asset>>> AddAssetsToRewardAsync(Guid rewardId, IDictionary<FileName, int> fileMap)
        {
            var reward = await FetchRewardAsync(rewardId);
            var assets = new List<Asset>();

            foreach (var entry in fileMap)
            {
                var asset = reward.AddAsset(entry.Key, entry.Value);
                assets.Add(asset);
            }

            await _repository.UpdateAsync(reward);
            return Tuple.Create<string, IReadOnlyCollection<Asset>>(reward.Title, assets);
        }

        public async Task RemoveAssetsFromRewardAsync(Guid rewardId, IEnumerable<FileName> fileNames)
        {
            var reward = await FetchRewardAsync(rewardId);
            foreach (var fileName in fileNames)
                reward.RemoveAsset(fileName.ToString());
            await _repository.UpdateAsync(reward);
        }
        #endregion

        #region Helpers
        private async Task<Entities.Reward> FetchRewardAsync(Guid rewardId)
        {
            return await FetchRewardByIdAsync(rewardId) ?? throw new EntityNotFoundException(rewardId.ToString());
        }

        private async Task<Entities.Reward?> FetchRewardByIdAsync(Guid rewardId)
        {
            if (rewardId == Guid.Empty)
                throw new ArgumentException("Invalid Reward Id.", nameof(rewardId));
            return await _repository.FindByIdAsync(rewardId.ToString());
        }
        #endregion
    }
}
