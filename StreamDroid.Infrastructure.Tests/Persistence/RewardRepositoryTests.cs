using StreamDroid.Core.Entities;
using StreamDroid.Core.Interfaces;
using StreamDroid.Infrastructure.Tests.Common;

namespace StreamDroid.Infrastructure.Tests.Persistence
{
    [Collection(TestCollectionFixture.Definition)]
    public class RewardRepositoryTests
    {
        private readonly IRepository<Reward> _repository;

        public RewardRepositoryTests(TestFixture testFixture)
        {
            _repository = testFixture.rewardRepository;
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task FindByIdAsync_Throws_InvalidArgs(string? id)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.FindByIdAsync(id));
        }

        [Fact]
        public async Task FindByIdAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var reward = await _repository.FindByIdAsync(id.ToString());

            Assert.NotNull(reward);
        }

        [Fact]
        public async Task FindAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var rewards = await _repository.FindAsync();

            Assert.NotEmpty(rewards);
        }

        [Fact]
        public async Task FindAsync_Expression()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var rewards = await _repository.FindAsync(x => x.Id.Equals(id.ToString()));

            Assert.NotEmpty(rewards);
        }

        [Theory]
        [InlineData(null)]
        public async Task AddAsync_Throws_InvalidArgs(Reward? reward)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.AddAsync(reward));
        }

        [Theory]
        [InlineData(null)]
        public async Task UpdateAsync_Throws_InvalidArgs(Reward? reward)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.UpdateAsync(reward));
        }

        [Fact]
        public async Task UpdateAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var reward = await _repository.FindByIdAsync(id.ToString());
            reward!.Title = "Test2";
            var updatedReward = await _repository.UpdateAsync(reward);

            Assert.Equal(reward.Title, updatedReward.Title);
        }

        [Fact]
        public async Task DeleteAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            await _repository.DeleteAsync(id.ToString());
            var reward = await _repository.FindByIdAsync(id.ToString());

            Assert.Null(reward);
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

            await _repository.AddAsync(reward);
        }
    }
}