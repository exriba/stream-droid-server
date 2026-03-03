using Microsoft.Extensions.Caching.Memory;
using Moq;
using SharpTwitch.Auth;
using SharpTwitch.Auth.Models;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Shared.Extensions;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Tests.Services.User
{
    [Collection(TestCollectionFixture.Definition)]
    public class UserManagerTests
    {
        private readonly Mock<IAuthApi> _mockAuthApi;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IUberRepository> _mockRepository;

        private readonly UserManager _userManager;

        public UserManagerTests(TestFixture testFixture)
        {
            _mockAuthApi = new Mock<IAuthApi>();
            _mockCache = new Mock<IMemoryCache>();
            _mockRepository = new Mock<IUberRepository>();

            _userManager = new UserManager(_mockAuthApi.Object, _mockCache.Object, testFixture.options, _mockRepository.Object);
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
            var refreshTokenResponse = new RefreshTokenResponse
            {
                AccessToken = "AccessToken",
                RefreshToken = user.RefreshToken,
            };

            _mockRepository.Setup(
                x => x.FindByIdAsync<Entities.User>(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.FromResult(user)!);
            _mockAuthApi.Setup(
                x => x.RefreshAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.FromResult(refreshTokenResponse));

            var mockEntry = new Mock<ICacheEntry>();
            mockEntry.SetupAllProperties();
            _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
                      .Returns(mockEntry.Object);

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
                RefreshToken = "refreshToken".Base64Encrypt()
            };
        }
        #endregion
    }
}
