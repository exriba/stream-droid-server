using StreamDroid.Core.Entities;
using StreamDroid.Core.Tests.Common;
using StreamDroid.Shared.Extensions;

namespace StreamDroid.Core.Tests.Entities
{
    public class UserTests : IClassFixture<TestFixture>
    {
        [Fact]
        public void User_Created()
        {
            var id = Guid.NewGuid().ToString();

            var user = new User
            {
                Id = id,
                Name = "User",
                AccessToken = "encryptMe",
                RefreshToken = "encryptMe"
            };

            Assert.Equal(id, user.Id);
            Assert.True(user.AccessToken.IsBase64String());
            Assert.True(user.RefreshToken.IsBase64String());
            Assert.Equal(100, user.Preferences.DefaultVolume);
            Assert.NotEqual(Guid.Empty, user.UserKey);
        }
    }
}
