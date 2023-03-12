using Ardalis.GuardClauses;
using StreamDroid.Domain.DTOs;
using StreamDroid.Infrastructure.Persistence;

namespace StreamDroid.Domain.Services.Redemption
{
    public sealed class RedemptionService : IRedemptionService
    {
        private readonly IRedemptionRepository _repository;

        public RedemptionService(IRedemptionRepository repository) 
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<RewardRedemptionDto>> FindRedemptionStatisticsByStreamerIdAsync(string streamerId)
        {
            Guard.Against.NullOrWhiteSpace(streamerId, nameof(streamerId));

            var redemptions = await _repository.FindAsync(x => x.Reward.StreamerId.Equals(streamerId));
            return redemptions.GroupBy(x => x.Reward, (x, y) =>
            {
                var value = decimal.Divide(y.Count(), redemptions.Count);
                var percentage = decimal.Multiply(value, 100); 
                var dto = RewardRedemptionDto.FromEntity(x);
                dto.Value = decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
                return dto;
            }).ToList();
        }

        public async Task<IReadOnlyList<UserRedemptionDto>> FindRedemptionStatisticsByRewardIdAsync(Guid rewardId)
        {
            if (rewardId == Guid.Empty)
                throw new ArgumentException("Invalid Reward Id.", nameof(rewardId));

            var redemptions = await _repository.FindAsync(x => x.Reward.Id.Equals(rewardId.ToString()));
            return redemptions.GroupBy(x => x.UserId, (x, y) =>
            {
                var value = decimal.Divide(y.Count(), redemptions.Count);
                var percentage = decimal.Multiply(value, 100);
                var dto = UserRedemptionDto.FromEntity(y.First());
                dto.Redeems = y.Count();
                dto.Percentage = decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
                return dto;
            }).ToList();
        }
    }
}
