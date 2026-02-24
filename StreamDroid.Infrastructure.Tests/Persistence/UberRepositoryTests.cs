using StreamDroid.Core.Entities;
using StreamDroid.Core.Enums;
using StreamDroid.Core.Interfaces;
using StreamDroid.Infrastructure.Tests.Common;

namespace StreamDroid.Infrastructure.Tests.Persistence
{
    [Collection(TestCollectionFixture.Definition)]
    public class UberRepositoryTests
    {
        private readonly IUberRepository _repository;

        public UberRepositoryTests(TestFixture testFixture)
        {
            _repository = testFixture.repository;
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task UberRepository_FindByIdAsync_Throws_InvalidArgs(string? id)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.FindByIdAsync<User>(id!));
        }

        [Fact]
        public async Task UberRepository_FindByIdAsync()
        {
            var user = CreateUser();

            await _repository.AddAsync(user);

            var entity = await _repository.FindByIdAsync<User>(user.Id);

            Assert.Equal(user, entity);
        }

        [Fact]
        public async Task UberRepository_FindListAsync()
        {
            var user = CreateUser();

            await _repository.AddAsync(user);

            var reward = CreateReward(user);
            var reward2 = CreateReward(user);

            await _repository.AddAsync(reward);
            await _repository.AddAsync(reward2);

            var entities = await _repository.FindListAsync<Reward>();

            Assert.NotEmpty(entities);
        }

        [Fact]
        public async Task UberRepository_FindStreamAsync()
        {
            var user = CreateUser();

            await _repository.AddAsync(user);

            var reward = CreateReward(user);
            var reward2 = CreateReward(user);
            var rewards = new List<Reward> { reward, reward2 };

            await _repository.AddAsync(reward);
            await _repository.AddAsync(reward2);

            await foreach (var entity in _repository.FindStreamAsync<Reward>())
            {
                Assert.Contains(entity, rewards);
            }
        }

        [Theory]
        [InlineData(null)]
        public async Task UberRepository_AddAsync_Throws_InvalidArgs(User? user)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.AddAsync(user!));
        }

        [Fact]
        public async Task UberRepository_AddAsync()
        {
            var user = CreateUser();

            var entity = await _repository.AddAsync(user);

            Assert.Equal(user, entity);
        }

        [Theory]
        [InlineData(null)]
        public async Task UberRepository_UpdateAsync_Throws_InvalidArgs(User? user)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.UpdateAsync(user!));
        }

        [Fact]
        public async Task UberRepository_UpdateAsync()
        {
            var user = CreateUser();

            await _repository.AddAsync(user);

            user.Name = "NewName";

            var entity = await _repository.UpdateAsync(user);

            Assert.Equal(user.Id, entity.Id);
            Assert.Equal("NewName", entity.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task UberRepository_DeleteAsync_Throws_InvalidArgs(string? id)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _repository.DeleteAsync<User>(id!));
        }

        [Fact]
        public async Task UberRepository_DeleteAsync()
        {
            var user = CreateUser();

            await _repository.AddAsync(user);

            await _repository.DeleteAsync<User>(user.Id);

            var entity = await _repository.FindByIdAsync<User>(user.Id);

            Assert.Null(entity);
        }

        #region Helpers
        private static User CreateUser()
        {
            var userId = Guid.NewGuid();

            return new User
            {
                Id = userId.ToString(),
                Name = "Name",
                UserType = UserType.NORMAL,
                AccessToken = "AccessToken",
                RefreshToken = "RefreshToken",
            };
        }

        private static Reward CreateReward(User user)
        {
            var rewardId = Guid.NewGuid();

            return new Reward
            {
                Id = rewardId.ToString(),
                ImageUrl = null,
                Title = "Title",
                Prompt = "Prompt",
                StreamerId = user.Id,
                BackgroundColor = "#6441A4",
            };
        }
        #endregion
    }
}
