using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.Services.Redeem;
using System.Linq.Expressions;
using System.Security.Claims;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Tests.Services.Redemption
{
    public class RedeemServiceTests
    {
        private readonly RedeemService _redeemService;
        private readonly ServerCallContext _context = TestServerCallContext.Create(
            method: "TestMethod",
            host: "localhost",
            deadline: DateTime.UtcNow.AddMinutes(1),
            requestHeaders: [],
            cancellationToken: CancellationToken.None,
            peer: "127.0.0.1",
            authContext: null,
            contextPropagationToken: null,
            writeHeadersFunc: (m) => Task.CompletedTask,
            writeOptionsGetter: () => null,
            writeOptionsSetter: (o) => { }
        );

        public RedeemServiceTests()
        {
            var redemptions = SetupRedemptions();

            var mockLogger = new Mock<ILogger<RedeemService>>();
            var mockRedeemRepository = new Mock<IRedemptionRepository>();
            mockRedeemRepository.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Entities.Redemption, bool>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(redemptions));

            _redeemService = new RedeemService(mockRedeemRepository.Object, mockLogger.Object);
        }

        [Fact]
        public async Task RedeemService_FindRewardRedeemStatisticsFromUser()
        {
            var id = Guid.NewGuid();
            ConfigureServerCallContext(id);

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

            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _redeemService.FindUserRedeemStatisticsByReward(request, _context));
        }

        [Fact]
        public async Task RedemptionService_FindUserRedeemStatisticsByReward()
        {
            var id = Guid.NewGuid();
            var request = new RewardRequest
            {
                RewardId = id.ToString()
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

        private void ConfigureServerCallContext(Guid id)
        {
            var httpContext = new DefaultHttpContext();
            var claimsIdentity = new ClaimsIdentity();
            var idClaim = new Claim("Id", id.ToString());
            var nameClaim = new Claim("Name", "Name");
            claimsIdentity.AddClaims([idClaim, nameClaim]);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            httpContext.User = claimsPrincipal;
            _context.UserState["__HttpContext"] = httpContext;
        }
        #endregion
    }
}
