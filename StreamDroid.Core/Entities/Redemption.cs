using StreamDroid.Core.Common;

namespace StreamDroid.Core.Entities
{
    /// <summary>
    /// An entity that contains reward redemption details. 
    /// </summary>
    public class Redemption : EntityBase
    {
        /// <summary>
        /// The user's ID that redeemed the reward
        /// </summary>
        public string UserId { get; init; } = string.Empty;
        
        /// <summary>
        /// The user's username that redeemed the reward
        /// </summary>
        public string UserName { get; init; } = string.Empty;

        /// <summary>
        /// The reward redeemed by the user
        /// </summary>
        public Reward Reward { get; init; } = new Reward();

        /// <summary>
        /// The datetime when the reward was redeemed
        /// </summary>
        public DateTime DateTime { get; private init; } = DateTime.Now;
    }
}
