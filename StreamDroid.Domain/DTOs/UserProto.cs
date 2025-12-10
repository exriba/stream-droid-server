using Grpc.Model;
using StreamDroid.Core.Enums;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.DTOs
{
    internal class UserProto : BaseProto<User, Entities.User>
    {
        public override void AddCustomMappings()
        {
            SetCustomMappings()
                .Map(dest => dest.UserKey, src => src.UserKey.ToString())
                .Map(dest => dest.UserType, src => ConvertFrom(src.UserType));
        }

        private static User.Types.UserType ConvertFrom(UserType userType)
        {
            bool converted = Enum.TryParse(userType.Name, true, out User.Types.UserType protoUserType);
            return converted ? protoUserType : User.Types.UserType.Unspecified;
        }
    }
}
