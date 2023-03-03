using StreamDroid.Core.Entities;
using StreamDroid.Infrastructure.Tests.Common;

namespace StreamDroid.Infrastructure.Tests.Persistence
{
    public class UberRepositoryTests : TestFixture
    {
        public UberRepositoryTests() : base() { }

        [Fact]
        public async Task FindAll()
        {
            // Given
            var rewards = CreateRewards();
            foreach (var reward in rewards)
                await _uberRepository.Save(reward);

            // When
            var data = await _uberRepository.FindAll<Reward>();

            // Then
            Assert.Equal(rewards.Count, data.Count);
        }

        [Theory]
        [InlineData(null)]
        public void Save_Throws_InvalidArgs(Reward reward)
        {
            Assert.ThrowsAnyAsync<ArgumentException>(async () => await _uberRepository.Save(reward));
        }

        [Fact]
        public async Task Save_Insert()
        {
            var rewards = CreateRewards();
            var reward = rewards.First();
            var entity = await _uberRepository.Save(reward);

            Assert.Equal(reward, entity);
        }

        [Fact]
        public async Task Save_Update()
        {
            var rewards = CreateRewards();
            var reward = rewards.First();
            await _uberRepository.Save(reward);

            var task = _uberRepository.Find<Reward>(r => r.Id.Equals(reward.Id));
            var entity = task.Result.First();
            entity.Title = "Updated";
            await _uberRepository.Save(entity);

            Assert.NotEqual(reward.Title, entity.Title);
        }

        [Theory]
        [InlineData(null)]
        public void Delete_Throws_InvalidArgs(Reward reward)
        {
            Assert.ThrowsAnyAsync<ArgumentException>(async () => await _uberRepository.Delete(reward));
        }

        [Fact]
        public async Task Delete()
        {
            var rewards = CreateRewards();
            var reward = rewards.First();

            await _uberRepository.Save(reward);
            await _uberRepository.Delete(reward);

            var data = await _uberRepository.Find<Reward>(r => r.Id.Equals(reward.Id));

            Assert.Empty(data);
        }

        private static IReadOnlyCollection<Reward> CreateRewards()
        {
            var streamerId = Guid.NewGuid().ToString();

            return new List<Reward>
            {
                new Reward
                {
                    Id = Guid.NewGuid().ToString(),
                    ImageUrl = null,
                    Title = "Title",
                    Prompt = "Prompt",
                    StreamerId = streamerId,
                    BackgroundColor = "#6441A4",
                },
                new Reward
                {
                    Id = Guid.NewGuid().ToString(),
                    ImageUrl = null,
                    Title = "Title",
                    Prompt = "Prompt",
                    StreamerId = streamerId,
                    BackgroundColor = "#6441A4",
                }
            };
        }
    }
}