using Grpc.Model;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.DTOs
{
    internal class RewardRedeemProto : BaseProto<RewardRedeem, Entities.Reward>
    {
        public override void AddCustomMappings()
        {
            SetCustomMappings()
                .Map(dest => dest.RewardId, src => src.Id)
                .Map(dest => dest.RewardTitle, src => src.Title)
                .Map(dest => dest.Fill, src => src.BackgroundColor);
        }
    }
}
