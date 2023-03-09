using StreamDroid.Core.Common;

namespace StreamDroid.Core.Entities
{
    public partial class Redemption : EntityBase
    {
        public string UserId { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public Reward Reward { get; init; } = new Reward();
        public DateTime DateTime { get; private init; } = DateTime.Now;
    }
}
