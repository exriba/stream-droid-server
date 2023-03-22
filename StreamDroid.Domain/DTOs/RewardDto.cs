using StreamDroid.Core.Entities;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Domain.DTOs
{
    /// <summary>
    /// DTO representation of a reward. See also <see cref="Reward"/>.
    /// </summary>
    public sealed class RewardDto : BaseDto<RewardDto, Reward>
    {
        public Guid Id { get; init; } = Guid.Empty;
        public string Title { get; init; } = string.Empty;
        public string Prompt { get; init; } = string.Empty;
        public string? ImageUrl { get; init; }
        public string BackgroundColor { get; init; } = string.Empty;
        public string StreamerId { get; init; } = string.Empty;
        public Speech Speech { get; init; } = new Speech();

        public override void AddCustomMappings()
        {
            SetCustomMappings()
                .Map(dest => dest.Id, src => src.Id.ToString());

            SetCustomMappingsInverse()
                .Map(dest => dest.Id, src => Guid.Parse(src.Id));
        }
    }
}
