using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.DTOs;

namespace StreamDroid.Domain.Services.Reward
{
    public interface IRewardService
    {
        #region Rewards
        Task<RewardDto> FindRewardByIdAsync(Guid rewardId);
        Task<IReadOnlyCollection<RewardDto>> FindRewardsByStreamerIdAsync(string userId);
        Task UpdateRewardSpeechAsync(Guid rewardId, Speech speech);
        Task SynchronizeRewardsAsync(string userId);
        #endregion

        #region Assets
        Task<IReadOnlyCollection<Asset>> FindAssetsByRewardIdAsync(Guid rewardId);
        Task RemoveAssetsFromRewardAsync(Guid rewardId, IEnumerable<FileName> fileNames);
        Task<Tuple<string, IReadOnlyCollection<Asset>>> AddAssetsToRewardAsync(Guid rewardId, IDictionary<FileName, int> fileMap);
        #endregion
    }
}
