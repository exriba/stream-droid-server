using StreamDroid.Domain.DTOs;

namespace StreamDroid.Domain.Services.Redemption
{
    /// <summary>
    /// Defines <see cref="Core.Entities.Redemption"/> business logic.
    /// </summary>
    public interface IRedemptionService
    {
        /// <summary>
        /// Finds redemptions by the given user id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>A collection of reward redemption DTOs.</returns>
        /// <exception cref="ArgumentNullException">If the user id is null</exception>
        /// <exception cref="ArgumentException">If the user id is empty or whitespace string</exception>
        Task<IReadOnlyList<RewardRedemptionDto>> FindRedemptionStatisticsByUserIdAsync(string userId);

        /// <summary>
        /// Finds redemptions by the given reward id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <returns>A collection of user redemption DTOs.</returns>
        /// <exception cref="ArgumentException">If the reward id is an empty GUID</exception>
        Task<IReadOnlyList<UserRedemptionDto>> FindRedemptionStatisticsByRewardIdAsync(Guid rewardId);
    }
}
