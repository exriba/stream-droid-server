using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;

namespace StreamDroid.Domain.Services.Reward
{
    public interface IRewardService
    {
        Task SyncRewards(string userId);
        Task<RewardDto> FindRewardById(string rewardId);
        Task UpdateRewardSpeech(string rewardId, Speech speech);
        Task<IReadOnlyCollection<Asset>> FindAssetsByRewardId(string rewardId);
        Task<IReadOnlyCollection<RewardDto>> FindRewardsByUserId(string userId);
        Task<Tuple<string, IReadOnlyCollection<Asset>>> AddRewardAssets(string rewardId, IDictionary<FileName, int> fileMap);
        Task RemoveRewardAssets(string rewardId, IEnumerable<FileName> fileNames);
    }
}
