using Grpc.Model;
using Entities = StreamDroid.Core.Entities;
using GrpcMediaExtension = Grpc.Model.Asset.Types.MediaExtension;

namespace StreamDroid.Domain.DTOs
{
    internal class RewardProto : BaseProto<Reward, Entities.Reward>
    {
        public override void AddCustomMappings()
        {
            SetCustomMappings()
                .MapWith(src => Convert(src));
        }

        private static Reward Convert(Entities.Reward src)
        {
            var dest = new Reward
            {
                Id = src.Id,
                Title = src.Title,
                Prompt = src.Prompt,
                ImageUrl = src.ImageUrl ?? string.Empty,
                BackgroundColor = src.BackgroundColor,
                StreamerId = src.StreamerId,
                Speech = new Speech
                {
                    Enabled = src.Speech.Enabled,
                    VoiceIndex = src.Speech.VoiceIndex
                }
            };
            var assets = src.Assets.Select(a =>
            {
                bool converted = Enum.TryParse(a.FileName.MediaExtension.Name, true, out GrpcMediaExtension mediaExtension);

                return new Asset
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = a.FileName.Name,
                    FileName = a.FileName.ToString(),
                    Volume = a.Volume,
                    MediaExtension = converted ? mediaExtension : GrpcMediaExtension.Unspecified
                };
            }).ToList();
            dest.Assets.AddRange(assets);
            return dest;
        }
    }
}
