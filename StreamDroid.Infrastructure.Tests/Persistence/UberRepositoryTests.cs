using Moq;
using StreamDroid.Core.Entities;
using StreamDroid.Infrastructure.Configuration;
using StreamDroid.Infrastructure.Persistence;

namespace StreamDroid.Infrastructure.Tests.Persistence
{
    public class UberRepositoryTests : IDisposable
    {
        private readonly string _filePath;
        private readonly UberRepository _uberRepository;

        public UberRepositoryTests()
        {
            _filePath = @$"{Directory.GetCurrentDirectory()}\test.db";
            var fileStream = new FileStream(_filePath, FileMode.Create);
            fileStream.Dispose();

            var persistenceSettings = new Mock<IPersistenceSettings>();
            persistenceSettings.Setup(x => x.ConnectionString).Returns($"Filename={_filePath}");
            _uberRepository = new UberRepository(persistenceSettings.Object);
        }

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

        [Fact]
        public void Save_Throws_NullEntity()
        {
            Assert.Throws<ArgumentNullException>(() => _uberRepository.Save<Reward>(null));
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

        [Fact]
        public void Delete_Throws_NullEntity()
        {
            Assert.Throws<ArgumentNullException>(() => _uberRepository.Delete<Reward>(null));
        }

        [Fact]
        public void Delete()
        {
            var rewards = CreateRewards();
            var reward = rewards.First();
            _uberRepository.Save(reward);

            _uberRepository.Delete(reward);
            var data = _uberRepository.Find<Reward>(r => r.Id.Equals(reward.Id));

            Assert.False(data.Any());
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

        public void Dispose()
        {
            _uberRepository.Dispose();
            File.Delete(_filePath);
        }
    }
}