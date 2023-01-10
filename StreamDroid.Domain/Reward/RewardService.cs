using Ardalis.GuardClauses;
using SharpTwitch.Core.Interfaces;
using SharpTwitch.Helix;
using StreamDroid.Core.Entities;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Helpers;
using StreamDroid.Domain.User;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Reward
{
    public sealed class RewardService : IRewardService
    {
        private readonly HelixApi _helixApi;
        private readonly IUserService _userService;
        private readonly IUberRepository _uberRepository;

        public RewardService(IApiCore apiCore, IUserService userService, ICoreSettings coreSettings, IUberRepository uberRepository)
        {
            _userService = userService;
            _uberRepository = uberRepository;
            _helixApi = new HelixApi(coreSettings, apiCore);
        }

        public Entities.Reward FindRewardById(string rewardId)
        {
            Guard.Against.NullOrWhiteSpace(rewardId, nameof(rewardId));
            var rewards = _uberRepository.Find<Entities.Reward>(r => r.Id.Equals(rewardId));

            if (rewards.Any())
                return rewards.First();

            throw new EntityNotFoundException(rewardId);
        }

        public IReadOnlyCollection<Entities.Reward> FindRewardsByUserId(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
            return _uberRepository.Find<Entities.Reward>(r => r.StreamerId.Equals(userId));
        }

        public Entities.Reward UpdateRewardSpeech(string rewardId, Speech speech)
        {
            var reward = FindRewardById(rewardId);
            reward.Speech = speech;
            _uberRepository.Save(reward);
            return reward;
        }

        public Tuple<string, IReadOnlyCollection<Asset>> AddRewardAssets(string rewardId, IDictionary<FileName, int> fileMap)
        {
            var reward = FindRewardById(rewardId);

            var assets = new List<Asset>();
            foreach (var entry in fileMap)
            {
                var asset = reward.AddAsset(entry.Key, entry.Value);
                assets.Add(asset);
            }

            _uberRepository.Save(reward);
            return Tuple.Create<string, IReadOnlyCollection<Asset>>(reward.Title, assets);
        }

        public void RemoveRewardAssets(string rewardId, IEnumerable<FileName> fileNames)
        {
            var reward = FindRewardById(rewardId);
            foreach(var fileName in fileNames)
                reward.RemoveAsset(fileName.ToString());
            _uberRepository.Save(reward);
        }

        public async Task SyncRewards(string userId)
        {
            var tokenRefreshPolicy = _userService.CreateTokenRefreshPolicy(userId);
            var data = await tokenRefreshPolicy.Policy.ExecuteAsync(async context =>
               await _helixApi.ChannelPoints.GetChannelPointRewards(userId, context[Constants.ACCESS_TOKEN].ToString(), CancellationToken.None), tokenRefreshPolicy.ContextData);

            if (data.Any())
            {
                var internalRewards = FindRewardsByUserId(userId);
                var externalRewards = data.Select(redeem =>
                {
                    var imageUrl = redeem.Image == null ? redeem.DefaultImage.Url1x : redeem.Image.Url1x;
                    var rewardBackgroundColor = redeem.BackgroundColor.Equals("#FFFFFF") ? "#6441A4" : redeem.BackgroundColor;
                    return new Entities.Reward
                    {
                        Id = redeem.Id,
                        ImageUrl = imageUrl,
                        Title = redeem.Title,
                        Prompt = redeem.Prompt,
                        StreamerId = redeem.BroadcasterId,
                        BackgroundColor = redeem.BackgroundColor,
                    };
                });

                var staleRewards = internalRewards.Except(externalRewards);
                foreach (var reward in staleRewards)
                    _uberRepository.Delete(reward);

                foreach (var reward in externalRewards)
                {
                    var rewards = _uberRepository.Find<Entities.Reward>(r => r.Id.Equals(reward.Id));

                    if (!rewards.Any())
                        _uberRepository.Save(reward);
                    else
                    {
                        var current = rewards.First();
                        current.Title = reward.Title;
                        current.Prompt = reward.Prompt;
                        current.ImageUrl = reward.ImageUrl;
                        current.BackgroundColor = reward.BackgroundColor;
                        _uberRepository.Save(current);
                    }
                }
            }
        }
    }
}
