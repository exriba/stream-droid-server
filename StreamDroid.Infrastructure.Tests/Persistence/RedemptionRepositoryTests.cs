using StreamDroid.Core.Entities;
using StreamDroid.Core.Interfaces;
using StreamDroid.Infrastructure.Tests.Common;

namespace StreamDroid.Infrastructure.Tests.Persistence
{
    [Collection(TestCollectionFixture.Definition)]
    public class RedemptionRepositoryTests
    {
        private readonly IRepository<Reward> _rewardRepository;
        private readonly IRedemptionRepository _redemptionRepository;

        public RedemptionRepositoryTests(TestFixture testFixture)
        {
            _rewardRepository = testFixture.rewardRepository;
            _redemptionRepository = testFixture.redemptionRepository;
        }

        [Fact]
        public async Task RedemptionRepository_FindAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var redemptions = await _redemptionRepository.FindAsync(x => x.Reward.Id.Equals(id.ToString()));

            Assert.NotEmpty(redemptions);
            foreach (var redemption in redemptions)
                Assert.NotNull(redemption.Reward);
        }

        private async Task SetupDataAsync(Guid id)
        {
            var reward = new Reward
            {
                Id = id.ToString(),
                ImageUrl = null,
                Title = "Title",
                Prompt = "Prompt",
                StreamerId = id.ToString(),
                BackgroundColor = "#6441A4",
            };

            reward = await _rewardRepository.AddAsync(reward);

            var redemptions = new List<Redemption>
            {
                {
                    new Redemption
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = id.ToString(),
                        UserName = "user",
                        Reward = reward
                    }
                },
                {
                    new Redemption
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = id.ToString(),
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
