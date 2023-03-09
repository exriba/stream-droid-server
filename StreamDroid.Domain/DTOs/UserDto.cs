using StreamDroid.Core.Entities;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Domain.DTOs
{
    public class UserDto : BaseDto<UserDto, User>
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public Guid UserKey { get; init; } = Guid.Empty;
        public Preferences Preferences { get; init; } = new Preferences();
    }
}
