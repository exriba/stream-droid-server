using StreamDroid.Core.Entities;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Infrastructure.Tests.Common;

namespace StreamDroid.Infrastructure.Tests.Persistence
{
    public class RewardRepositoryTests : IClassFixture<TestFixture>
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
        public async Task FindByIdAsync_Throws_InvalidArgs(string id)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.FindByIdAsync(id));
        }

        [Fact]
        public async Task FindByIdAsync()
        {
            var id = Guid.NewGuid();
            await CreateReward(id);

            var reward = await _repository.FindByIdAsync(id.ToString());

            Assert.NotNull(reward);
        }

        [Fact]
        public async Task FindAsync()
        {
            var id = Guid.NewGuid();
            await CreateReward(id);

            var rewards = await _repository.FindAsync();

            Assert.NotEmpty(rewards);
        }

        [Fact]
        public async Task FindAsync_Expression()
        {
            var id = Guid.NewGuid();
            await CreateReward(id);

            var rewards = await _repository.FindAsync(x => x.Id.Equals(id.ToString()));

            Assert.NotEmpty(rewards);
        }

        [Theory]
        [InlineData(null)]
        public async Task AddAsync_Throws_InvalidArgs(Reward reward)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.AddAsync(reward));
        }

        [Theory]
        [InlineData(null)]
        public async Task UpdateAsync_Throws_InvalidArgs(Reward reward)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.UpdateAsync(reward));
        }

        [Fact]
        public async Task UpdateAsync()
        {
            var id = Guid.NewGuid();
            var reward = await CreateReward(id);
            reward.Title = "Test2";

            var updatedReward = await _repository.UpdateAsync(reward);

            Assert.Equal(reward.Title, updatedReward.Title);
        }

        [Fact]
        public async Task DeleteAsync()
        {
            var id = Guid.NewGuid();
            var reward = await CreateReward(id);

            await _repository.DeleteAsync(id.ToString());
            reward = await _repository.FindByIdAsync(id.ToString());

            Assert.Null(reward);
        }

        private async Task<Reward> CreateReward(Guid id)
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

            return await _repository.AddAsync(reward);
        }
    }
}