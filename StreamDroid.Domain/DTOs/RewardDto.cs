using StreamDroid.Core.Entities;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Domain.DTOs
{
    public class RewardDto : BaseDto<RewardDto, Reward>
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Title { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string BackgroundColor { get; set; } = string.Empty;
        public string StreamerId { get; set; } = string.Empty;
        public Speech Speech { get; set; } = new Speech();

        public override void AddCustomMappings()
        {
            SetCustomMappings()
                .Map(dest => dest.Id, src => src.Id.ToString());

            SetCustomMappingsInverse()
                .Map(dest => dest.Id, src => Guid.Parse(src.Id));
        }
    }
}
