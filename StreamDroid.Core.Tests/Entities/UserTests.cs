using StreamDroid.Core.Entities;
using StreamDroid.Core.Tests.Common;
using StreamDroid.Shared.Helpers;

namespace StreamDroid.Core.Tests.Entities
{
    public class UserTests : TestFixture
    {
        public UserTests() : base() { }

        [Fact]
        public void User_Throws_NullFields()
        {
            Assert.Throws<ArgumentNullException>(() => new User { Id = null });
            Assert.Throws<ArgumentNullException>(() => new User { Name = null });
            Assert.Throws<ArgumentNullException>(() => new User { AccessToken = null });
            Assert.Throws<ArgumentNullException>(() => new User { RefreshToken = null });
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void User_Throws_InvalidFields(string value)
        {
            Assert.Throws<ArgumentException>(() => new User { Id = value });
            Assert.Throws<ArgumentException>(() => new User { Name = value });
            Assert.Throws<ArgumentException>(() => new User { AccessToken = value });
            Assert.Throws<ArgumentException>(() => new User { RefreshToken = value });
        }

        [Fact]
        public void User_Created()
        {
            var id = Guid.NewGuid().ToString();

            var user = new User
            {
                Id = id,
                Name = "User",
                UserKey = null,
                AccessToken = "encryptMe",
                RefreshToken = "encryptMe"
            };

            Assert.Equal(id, user.Id);
            Assert.NotNull(user.UserKey);
            Assert.True(user.AccessToken.IsBase64String());
            Assert.True(user.RefreshToken.IsBase64String());
        }
    }
}
