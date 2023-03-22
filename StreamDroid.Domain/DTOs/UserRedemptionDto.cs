using StreamDroid.Core.Entities;

namespace StreamDroid.Domain.DTOs
{
    /// <summary>
    /// DTO representation of a user redemption. See also <see cref="Redemption"/>.
    /// </summary>
    public sealed class UserRedemptionDto : BaseDto<UserRedemptionDto, Redemption>
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Redeems { get; set; }
        public decimal Percentage { get; set; }

        public override void AddCustomMappings()
        {
            SetCustomMappingsInverse()
                .Map(dest => dest.Id, src => int.Parse(src.UserId));
        }
    }
}
