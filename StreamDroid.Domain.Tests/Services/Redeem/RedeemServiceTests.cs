using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.Services.Redeem;
using StreamDroid.Domain.Tests.Common;
using System.Linq.Expressions;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Tests.Services.Redemption
{
    [Collection(TestCollectionFixture.Definition)]
    public class RedeemServiceTests
    {
        private readonly ServerCallContext _context;
        private readonly RedeemService _redeemService;

        public RedeemServiceTests(TestFixture testFixture)
        {
            var redemptions = SetupRedemptions();
            _context = testFixture.createTestServerCallContext(null);

            var mockLogger = new Mock<ILogger<RedeemService>>();
            var mockRepository = new Mock<IUberRepository>();
            mockRepository.Setup(
                x => x.FindListAsync(
                    It.IsAny<Expression<Func<Entities.Redemption, bool>>>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.FromResult(redemptions));

            _redeemService = new RedeemService(mockRepository.Object, mockLogger.Object);
        }

        [Fact]
        public async Task RedeemService_FindRewardRedeemStatisticsFromUser()
        {
            var request = new Google.Protobuf.WellKnownTypes.Empty();

            var response = await _redeemService.FindRewardRedeemStatisticsFromUser(request, _context);
            var rewardRedeem = response.RewardRedeems.Single();

            Assert.Equal("100", rewardRedeem.Percentage);
        }

        [Fact]
        public async Task RedemptionService_FindUserRedeemStatisticsByReward_Throws_InvalidArgs()
        {
            var request = new RewardRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            await Assert.ThrowsAnyAsync<ArgumentException>(
                async () => await _redeemService.FindUserRedeemStatisticsByReward(request, _context)
            );
        }

        [Fact]
        public async Task RedemptionService_FindUserRedeemStatisticsByReward()
        {
            var request = new RewardRequest
            {
                RewardId = Guid.NewGuid().ToString()
            };

            var response = await _redeemService.FindUserRedeemStatisticsByReward(request, _context);
            var userRedeems = response.UserRedeems;

            Assert.NotEmpty(userRedeems);

            var userRedeem = userRedeems.First();

            Assert.Equal(2, userRedeem.RedeemCount);
            Assert.Equal("100", userRedeem.Percentage);
        }

        #region Helpers
        private static Entities.Reward SetupReward()
        {
            var id = Guid.NewGuid();

            return new Entities.Reward
            {
                Id = id.ToString(),
                ImageUrl = null,
                Title = "Title",
                Prompt = "Prompt",
                StreamerId = id.ToString(),
                BackgroundColor = "#6441A4",
            };
        }

        private static IReadOnlyCollection<Entities.Redemption> SetupRedemptions()
        {
            var reward = SetupReward();

            return new List<Entities.Redemption>
            {
                {
                    new Entities.Redemption
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = "123",
                        UserName = "user",
                        Reward = reward
                    }
                },
                {
                    new Entities.Redemption
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = "123",
                        UserName = "user",
                        Reward = reward
                    }
                }
            };
        }
        #endregion
    }
}
