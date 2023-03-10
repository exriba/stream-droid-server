using Ardalis.SmartEnum;

namespace StreamDroid.Core.Enums
{
    public class UserType : SmartEnum<UserType>
    {
        public static readonly UserType NORMAL = new(nameof(NORMAL), 0);
        public static readonly UserType AFFILIATE = new(nameof(AFFILIATE), 1);
        public static readonly UserType PARTNER = new(nameof(PARTNER), 2);

        protected UserType(string name, int value) : base(name, value) { }

        public override string ToString() => $"{Name.ToLower()}";
    }
}
