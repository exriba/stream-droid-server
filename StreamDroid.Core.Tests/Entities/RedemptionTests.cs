using StreamDroid.Core.Entities;

namespace StreamDroid.Core.Tests.Entities
{
    public class RedemptionTests
    {
        [Fact]
        public void Redemption_Created()
        {
            var id = Guid.NewGuid().ToString();
            var userId = Guid.NewGuid().ToString();
            var userName = "Test";

            var redemption = new Redemption
            {
                Id = id,
                UserId = userId,
                UserName = userName,
                Reward = new Reward
                {
                    StreamerId = userId
                }
            };

            Assert.Equal(id, redemption.Id);
            Assert.Equal(userId, redemption.UserId);
            Assert.Equal(userName, redemption.UserName);
            Assert.Equal(userId, redemption.Reward.StreamerId);
            Assert.NotEqual(DateTime.MinValue, redemption.DateTime);

        }
    }
}
