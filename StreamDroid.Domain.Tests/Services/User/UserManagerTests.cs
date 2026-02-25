using Moq;
using SharpTwitch.Auth;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Tests.Common;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Tests.Services.User
{
    [Collection(TestCollectionFixture.Definition)]
    public class UserManagerTests
    {
        private readonly Mock<IUberRepository> _mockRepository;

        private readonly UserManager _userManager;

        public UserManagerTests(TestFixture testFixture)
        {
            var mockAuthApi = new Mock<IAuthApi>();
            _mockRepository = new Mock<IUberRepository>();

            _userManager = new UserManager(mockAuthApi.Object, testFixture.options, _mockRepository.Object);
        }

        [Fact]
        public async Task UserManager_FetchUserByIdAsync_Throws_EntityNotFoundException()
        {
            Entities.User? user = null;

            _mockRepository.Setup(
                x => x.FindByIdAsync<Entities.User>(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.FromResult(user));

            await Assert.ThrowsAnyAsync<EntityNotFoundException>(
                async () => await _userManager.FetchUserByIdAsync(string.Empty, CancellationToken.None)
            );
        }

        [Fact]
        public async Task UserManager_FetchUserByIdAsync()
        {
            var user = SetupUser();

            _mockRepository.Setup(
                x => x.FindByIdAsync<Entities.User>(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.FromResult(user)!);

            var result = await _userManager.FetchUserByIdAsync(user.Id);

            Assert.Equal(user, result);
        }

        [Fact]
        public async Task UserManager_GenerateAccessTokenAsync()
        {
            var user = SetupUser();

            _mockRepository.Setup(
                x => x.FindByIdAsync<Entities.User>(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.FromResult(user)!);

            var token = await _userManager.GenerateAccessTokenAsync(user.Id, CancellationToken.None);

            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task UserManager_CreateTokenRefreshPolicyAsync()
        {
            var user = SetupUser();

            _mockRepository.Setup(
                x => x.FindByIdAsync<Entities.User>(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.FromResult(user)!);

            var policy = await _userManager.CreateTokenRefreshPolicyAsync(user.Id);

            Assert.Equal(2, policy.ContextData.Keys.Count);
        }

        #region Helpers
        private static Entities.User SetupUser()
        {
            var id = Guid.NewGuid();

            return new Entities.User
            {
                Id = id.ToString(),
                Name = "user",
                AccessToken = "accessToken",
                RefreshToken = "refreshToken"
            };
        }
        #endregion
    }
}
