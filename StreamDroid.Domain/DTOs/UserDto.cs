using StreamDroid.Core.Entities;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Domain.DTOs
{
    public class UserDto : BaseDto<UserDto, User>
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid UserKey { get; set; } = Guid.Empty;
        public Preferences Preferences { get; set; } = new Preferences();

        public override void AddCustomMappings()
        {
            SetCustomMappings()
                .Map(dest => dest.UserKey, src => src.UserKey.ToString());

            SetCustomMappingsInverse()
                .Map(dest => dest.UserKey, src => string.IsNullOrWhiteSpace(src.UserKey) ? Guid.Empty : Guid.Parse(src.UserKey));
        }
    }
}
