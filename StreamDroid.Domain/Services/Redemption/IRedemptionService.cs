using StreamDroid.Domain.DTOs;

namespace StreamDroid.Domain.Services.Redemption
{
    public interface IRedemptionService
    {
        Task<IReadOnlyList<RewardRedemptionDto>> FindRedemptionStatisticsByStreamerIdAsync(string streamerId);
        Task<IReadOnlyList<UserRedemptionDto>> FindRedemptionStatisticsByRewardIdAsync(Guid rewardId);
    }
}
