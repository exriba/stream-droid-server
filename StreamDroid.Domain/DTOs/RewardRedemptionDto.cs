using StreamDroid.Core.Entities;

namespace StreamDroid.Domain.DTOs
{
    /// <summary>
    /// DTO representation of a reward redemption. See also <see cref="Redemption"/>.
    /// </summary>
    public sealed class RewardRedemptionDto : BaseDto<RewardRedemptionDto, Reward>
    {
        public string Name { get; init; } = string.Empty;
        public string Fill { get; init; } = string.Empty;
        public decimal Value { get; set; }

        public override void AddCustomMappings()
        {
            SetCustomMappingsInverse()
                .Map(dest => dest.Name, src => src.Title)
                .Map(dest => dest.Fill, src => src.BackgroundColor);
        }
    }
}
