using Ardalis.SmartEnum;
using StreamDroid.Core.Enums;

namespace StreamDroid.Core.Tests.Enums
{
    public class UserTypeTests
    {
        private const string NORMAL = "NORMAL";
        private const string AFFILIATE = "AFFILIATE";
        private const string PARTNER = "PARTNER";
        private const string UNKNOWN = "UNKNOWN";

        [Theory]
        [InlineData(-1)]
        public void UserType_FromValue_Throws_InvalidArgs(int value)
        {
            Assert.ThrowsAny<SmartEnumNotFoundException>(() => UserType.FromValue(value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void UserType_FromValue(int value)
        {
            var userType = UserType.FromValue(value);

            Assert.NotNull(userType);
        }

        [Theory]
        [InlineData(UNKNOWN)]
        public void UserType_FromName_Throws_InvalidArgs(string name)
        {
            Assert.ThrowsAny<SmartEnumNotFoundException>(() => UserType.FromName(name));
        }

        [Theory]
        [InlineData(NORMAL)]
        [InlineData(AFFILIATE)]
        [InlineData(PARTNER)]
        public void UserType_FromName(string name)
        {
            var userType = UserType.FromName(name);

            Assert.NotNull(userType);
        }
    }
}
