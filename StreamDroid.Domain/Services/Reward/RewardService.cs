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
        private readonly IUberRepository _uberRepository;

        public RewardService(HelixApi helixApi, IUserService userService, IUberRepository uberRepository)
        {
            _helixApi = helixApi;
            _userService = userService;
            _uberRepository = uberRepository;
        }

        public async Task<RewardDto> FindRewardById(string rewardId)
        {
            var reward = await FindById(rewardId);
            return RewardDto.FromEntity(reward);
        }

        public async Task<IReadOnlyCollection<Asset>> FindAssetsByRewardId(string rewardId)
        {
            var reward = await FindById(rewardId);
            return reward.Assets;
        }

        public async Task<IReadOnlyCollection<RewardDto>> FindRewardsByUserId(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

            var entities = await _uberRepository.Find<Entities.Reward>(r => r.StreamerId.Equals(userId));
            var rewards = entities.AsQueryable(); 
            return rewards.ProjectToType<RewardDto>().ToList();
        }

        public async Task UpdateRewardSpeech(string rewardId, Speech speech)
        {
            var reward = await FindById(rewardId);

            reward.Speech = speech;
            await _uberRepository.Save(reward);
        }

        public async Task<Tuple<string, IReadOnlyCollection<Asset>>> AddRewardAssets(string rewardId, IDictionary<FileName, int> fileMap)
        {
            var reward = await FindById(rewardId);
            var assets = new List<Asset>();

            foreach (var entry in fileMap)
            {
                var asset = reward.AddAsset(entry.Key, entry.Value);
                assets.Add(asset);
            }

            await _uberRepository.Save(reward);

            return Tuple.Create<string, IReadOnlyCollection<Asset>>(reward.Title, assets);
        }

        public async Task RemoveRewardAssets(string rewardId, IEnumerable<FileName> fileNames)
        {
            var reward = await FindById(rewardId);

            foreach (var fileName in fileNames)
                reward.RemoveAsset(fileName.ToString());

            await _uberRepository.Save(reward);
        }

        public async Task SyncRewards(string userId)
        {
            var tokenRefreshPolicy = await _userService.CreateTokenRefreshPolicy(userId);

            var users = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
                await _helixApi.Users.GetUsersAsync(Array.Empty<string>(), tokenRefreshPolicy.AccessToken, CancellationToken.None), tokenRefreshPolicy.ContextData);

            if (users.Any())
            {
                var user = users.First();

                if (user.UserBroadcasterType is not BroadcasterType.NORMAL)
                {
                    var data = await _helixApi.CustomRewards.GetCustomRewardsAsync(userId, tokenRefreshPolicy.AccessToken, CancellationToken.None);

                    var externalRewards = data.Select(customReward =>
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

                    foreach (var reward in externalRewards)
                    {
                        var rewards = await _uberRepository.Find<Entities.Reward>(r => r.Id.Equals(reward.Id));

                        if (!rewards.Any())
                            await _uberRepository.Save(reward);
                        else
                        {
                            var current = rewards.First();
                            current.Title = reward.Title;
                            current.Prompt = reward.Prompt;
                            current.ImageUrl = reward.ImageUrl;
                            current.BackgroundColor = reward.BackgroundColor;
                            current.Speech = new Speech(reward.Speech.Enabled, current.Speech.VoiceIndex);
                            await _uberRepository.Save(current);
                        }
                    }
                }
            }
        }

        private async Task<Entities.Reward> FindById(string rewardId)
        {
            Guard.Against.NullOrWhiteSpace(rewardId, nameof(rewardId));

            var rewards = await _uberRepository.Find<Entities.Reward>(r => r.Id.Equals(rewardId));
            return rewards.Any() ? rewards.First() : throw new EntityNotFoundException(rewardId);
        }
    }
}
