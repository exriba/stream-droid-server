using StreamDroid.Core.ValueObjects;
using StreamDroid.Core.Exceptions;
using StreamDroid.Domain.DTOs;

namespace StreamDroid.Domain.Services.Reward
{
    /// <summary>
    /// Defines <see cref="Core.Entities.Reward"/> business logic.
    /// </summary>
    public interface IRewardService : IDisposable, IAsyncDisposable
    {
        #region Rewards
        /// <summary>
        /// Finds a reward by the given id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <returns>A reward DTO.</returns>
        /// <exception cref="EntityNotFoundException">If the reward is not found</exception>
        Task<RewardDto> FindRewardByIdAsync(Guid rewardId);

        /// <summary>
        /// Finds a collection of rewards by the given user id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>A collection of reward DTOs.</returns>
        /// <exception cref="ArgumentNullException">If the user id is null</exception>
        /// <exception cref="ArgumentException">If the user id is an empty or whitespace string</exception>
        Task<IReadOnlyCollection<RewardDto>> FindRewardsByUserIdAsync(string userId);

        /// <summary>
        /// Updates the speech for the given reward.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="speech">speech</param>
        /// <exception cref="EntityNotFoundException">If the reward is not found</exception>
        Task UpdateRewardSpeechAsync(Guid rewardId, Speech speech);

        /// <summary>
        /// Synchronizes external rewards for the given user.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <exception cref="ArgumentNullException">If the user id is null</exception>
        /// <exception cref="ArgumentException">If the user id is an empty or whitespace string</exception>
        Task SynchronizeRewardsAsync(string userId);
        #endregion

        #region Assets
        /// <summary>
        /// Finds assets by the given reward id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <returns>A collection of assets.</returns>
        /// <exception cref="EntityNotFoundException">If the reward is not found</exception>
        Task<IReadOnlyCollection<Asset>> FindAssetsByRewardIdAsync(Guid rewardId);

        /// <summary>
        /// Removes assets from the given reward.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="fileNames">file names</param>
        /// <exception cref="EntityNotFoundException">If the reward is not found</exception>
        Task RemoveAssetsFromRewardAsync(Guid rewardId, IEnumerable<FileName> fileNames);

        /// <summary>
        /// Adds assets to the given reward.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="fileMap">file map</param>
        /// <returns>A tuple of reward title and a collection of assets.</returns>
        /// <exception cref="EntityNotFoundException">If the reward is not found</exception>
        Task<Tuple<string, IReadOnlyCollection<Asset>>> AddAssetsToRewardAsync(Guid rewardId, IDictionary<FileName, int> fileMap);
        #endregion
    }
}
