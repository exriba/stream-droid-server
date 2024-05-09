using StreamDroid.Core.Entities;
using StreamDroid.Core.Enums;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Domain.DTOs
{
    /// <summary>
    /// DTO representation of a user. See also <see cref="User"/>.
    /// </summary>
    public sealed class UserDto : BaseDto<UserDto, User>
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public Guid UserKey { get; init; } = Guid.Empty;
        public UserType UserType { get; init; } = UserType.NORMAL;
        public Preferences Preferences { get; init; } = new Preferences();
    }
}
