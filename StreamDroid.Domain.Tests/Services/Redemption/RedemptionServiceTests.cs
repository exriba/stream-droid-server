using StreamDroid.Domain.Services.Redemption;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Infrastructure.Persistence;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Tests.Services.Redemption
{
    [Collection(TestCollectionFixture.Definition)]
    public class RedemptionServiceTests
    {
        private readonly RedemptionService _redemptionService;
        private readonly IRedemptionRepository _redemptionRepository;
        private readonly IRepository<Entities.Reward> _rewardRepository;

        public RedemptionServiceTests(TestFixture testFixture)
        {
            _rewardRepository = testFixture.rewardRepository;
            _redemptionRepository = testFixture.redemptionRepository;
            _redemptionService = new RedemptionService(_redemptionRepository);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task RedemptionService_FindRedemptionStatisticsByUserIdAsync_Throws_InvalidArgs(string? id)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _redemptionService.FindRedemptionStatisticsByUserIdAsync(id));
        }

        [Fact]
        public async Task RedemptionService_FindRedemptionStatisticsByUserIdAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var rewardRedemptionDtos = await _redemptionService.FindRedemptionStatisticsByUserIdAsync(id.ToString());

            Assert.NotEmpty(rewardRedemptionDtos);

            var rewardRedemptionDto = rewardRedemptionDtos[0];

            Assert.Equal(100, rewardRedemptionDto.Value);
        }

        [Fact]
        public async Task RedemptionService_FindRedemptionStatisticsByRewardIdAsync_Throws_InvalidArgs()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _redemptionService.FindRedemptionStatisticsByRewardIdAsync(Guid.Empty));
        }

        [Fact]
        public async Task RedemptionService_FindRedemptionStatisticsByRewardIdAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var userRedemptionDtos = await _redemptionService.FindRedemptionStatisticsByRewardIdAsync(id);

            Assert.NotEmpty(userRedemptionDtos);

            var userRedemptionDto = userRedemptionDtos[0];

            Assert.Equal(2, userRedemptionDto.Redeems);
            Assert.Equal(100, userRedemptionDto.Percentage);
        }

        private async Task SetupDataAsync(Guid id)
        {
            var reward = new Entities.Reward
            {
                Id = id.ToString(),
                ImageUrl = null,
                Title = "Title",
                Prompt = "Prompt",
                StreamerId = id.ToString(),
                BackgroundColor = "#6441A4",
            };

            reward = await _rewardRepository.AddAsync(reward);

            var redemptions = new List<Entities.Redemption>
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

            foreach (var redemption in redemptions)
                await _redemptionRepository.AddAsync(redemption);
        }
    }
}
