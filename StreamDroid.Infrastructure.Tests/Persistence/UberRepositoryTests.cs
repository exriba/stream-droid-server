using StreamDroid.Core.Entities;
using StreamDroid.Infrastructure.Tests.Common;

namespace StreamDroid.Infrastructure.Tests.Persistence
{
    public class UberRepositoryTests : TestFixture
    {
        public UberRepositoryTests() : base() { }

        [Fact]
        public void FindAll()
        {
            // Given
            var rewards = CreateRewards();
            foreach (var reward in rewards)
                _uberRepository.Save(reward);

            // When
            var data = _uberRepository.FindAll<Reward>();

            // Then
            Assert.Equal(rewards.Count, data.Count);
        }

        [Theory]
        [InlineData(null)]
        public void Save_Throws_InvalidArgs(Reward reward)
        {
            Assert.ThrowsAny<ArgumentException>(() => _uberRepository.Save(reward));
        }

        [Fact]
        public void Save_Insert()
        {
            var rewards = CreateRewards();
            var reward = rewards.First();
            var entity = _uberRepository.Save(reward);

            Assert.Equal(reward, entity);
        }

        [Fact]
        public void Save_Update()
        {
            var rewards = CreateRewards();
            var reward = rewards.First();
            _uberRepository.Save(reward);

            var data = _uberRepository.Find<Reward>(r => r.Id.Equals(reward.Id));
            var entity = data.First();
            entity.Title = "Updated";
            _uberRepository.Save(entity);

            Assert.NotEqual(reward.Title, entity.Title);
        }

        [Theory]
        [InlineData(null)]
        public void Delete_Throws_InvalidArgs(Reward reward)
        {
            Assert.ThrowsAny<ArgumentException>(() => _uberRepository.Delete(reward));
        }

        [Fact]
        public void Delete()
        {
            var rewards = CreateRewards();
            var reward = rewards.First();
            _uberRepository.Save(reward);

            _uberRepository.Delete(reward);
            var data = _uberRepository.Find<Reward>(r => r.Id.Equals(reward.Id));

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