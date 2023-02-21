using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;

namespace StreamDroid.Domain.Services.Reward
{
    public interface IRewardService
    {
        Task SyncRewards(string userId);
        RewardDto FindRewardById(string rewardId);
        void UpdateRewardSpeech(string rewardId, Speech speech);
        IReadOnlyCollection<Asset> FindAssetsByRewardId(string rewardId);
        IReadOnlyCollection<RewardDto> FindRewardsByUserId(string userId);
        Tuple<string, IReadOnlyCollection<Asset>> AddRewardAssets(string rewardId, IDictionary<FileName, int> fileMap);
        void RemoveRewardAssets(string rewardId, IEnumerable<FileName> fileNames);
    }
}
