using Ardalis.SmartEnum;

namespace StreamDroid.Core.Enums
{
    /// <summary>
    /// User type enum.
    /// </summary>
    public class UserType : SmartEnum<UserType>
    {
        /// <summary>
        /// Normal
        /// </summary>
        public static readonly UserType NORMAL = new(nameof(NORMAL), 0);
        
        /// <summary>
        /// Affiliate
        /// </summary>
        public static readonly UserType AFFILIATE = new(nameof(AFFILIATE), 1);
        
        /// <summary>
        /// Partner
        /// </summary>
        public static readonly UserType PARTNER = new(nameof(PARTNER), 2);

        protected UserType(string name, int value) : base(name, value) { }

        public override string ToString() => $"{Name.ToLower()}";
    }
}
