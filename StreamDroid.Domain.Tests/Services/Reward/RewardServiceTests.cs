using Moq;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Settings;
using SharpTwitch.Helix.Models;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Services.Reward;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Tests.Common;
using SharpTwitch.Core;
using SharpTwitch.Helix.Models.Shared;
using SharpTwitch.Helix;
using StreamDroid.Domain.RefreshPolicy;
using SharpTwitch.Helix.Models.Channel.Reward;
using Entities = StreamDroid.Core.Entities;
using Helix = SharpTwitch.Helix.Models;
using StreamDroid.Infrastructure.Persistence;

namespace StreamDroid.Domain.Tests.Services.Reward
{
    [Collection(TestCollectionFixture.Definition)]
    public class RewardServiceTests
    {
        private readonly Mock<IApiCore> _apiCore;
        private readonly RewardService _rewardService;
        private readonly Mock<IUserService> _userService;
        private readonly IRepository<Entities.Reward> _rewardRepository;

        public RewardServiceTests(TestFixture testFixture)
        {
            _apiCore = new Mock<IApiCore>();
            _userService = new Mock<IUserService>();
            var coreSettings = new Mock<ICoreSettings>();
            _rewardRepository = testFixture.rewardRepository;
            var helixApi = new HelixApi(coreSettings.Object, _apiCore.Object);
            _rewardService = new RewardService(helixApi, _userService.Object, _rewardRepository);
        }

        [Fact]
        public async Task RewardService_FindRewardByIdAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var rewardDto = await _rewardService.FindRewardByIdAsync(id);

            Assert.Equal(id, rewardDto.Id);
        }

        [Fact]
        public async Task RewardService_FindAssetsByRewardIdAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var assets = await _rewardService.FindAssetsByRewardIdAsync(id);

            Assert.NotEmpty(assets);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task RewardService_FindRewardsByStreamerIdAsync_Throws_InvalidArgs(string userId)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _rewardService.FindRewardsByStreamerIdAsync(userId));
        }

        [Fact]
        public async Task RewardService_FindRewardsByStreamerIdAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);

            var reward = await _rewardRepository.FindByIdAsync(id.ToString());
            var rewards = await _rewardService.FindRewardsByStreamerIdAsync(reward!.StreamerId);

            Assert.NotEmpty(rewards);
        }

        [Fact]
        public async Task RewardService_UpdateRewardSpeechAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            var speech = new Speech(true);

            await _rewardService.UpdateRewardSpeechAsync(id, speech);
            var rewardDto = await _rewardService.FindRewardByIdAsync(id);

            Assert.True(rewardDto.Speech.Enabled);
        }

        [Fact]
        public async Task RewardService_AddAssetsToRewardAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            var dictionary = new Dictionary<FileName, int>
            {
                { FileName.FromString("file.mp4"), 100 },
            };

            var tuple = await _rewardService.AddAssetsToRewardAsync(id, dictionary);

            Assert.Equal("Title", tuple.Item1);
            Assert.NotEmpty(tuple.Item2);
        }

        [Fact]
        public async Task RewardService_RemoveAssetsFromRewardAsync()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            var dictionary = new Dictionary<FileName, int>
            {
                { FileName.FromString("file.mp4"), 100 },
            };

            await _rewardService.AddAssetsToRewardAsync(id, dictionary);
            var assets = await _rewardService.FindAssetsByRewardIdAsync(id);

            Assert.Equal(2, assets.Count);

            await _rewardService.RemoveAssetsFromRewardAsync(id, dictionary.Keys);
            assets = await _rewardService.FindAssetsByRewardIdAsync(id);

            Assert.Equal(1, assets.Count);
        }

        [Fact]
        public async Task RewardService_SynchronizeRewardsAsync()
        {
            var user = new Helix.User.User
            {
                Id = Guid.NewGuid().ToString(),
                BroadcasterType = "affiliate"
            };

            var userResponse = new HelixCollectionResponse<Helix.User.User>
            {
                Data = new List<Helix.User.User> { user }
            };

            var rewardId = Guid.NewGuid();
            var customReward = new CustomReward
            {
                Id = rewardId.ToString(),
                Title = "Title",
                Prompt = "Prompt",
                BroadcasterUserId = user.Id,
                BackgroundColor = "#FFFFFF",
                IsUserInputRequired = true,
                Image = new Image
                {
                    Url1x = "http://localhost/image.png"
                },
            };

            var customRewardResponse = new HelixCollectionResponse<CustomReward>
            {
                Data = new List<CustomReward> { customReward }
            };

            static async Task<string> refreshToken(string userId) => await Task.FromResult("NewAccessToken");
            var tokenRefreshPolicy = new TokenRefreshPolicy(user.Id, "accessToken", refreshToken);

            _userService.Setup(x => x.CreateTokenRefreshPolicyAsync(It.IsAny<string>()))
                        .ReturnsAsync(tokenRefreshPolicy);
            _apiCore.Setup(x => x.GetAsync<HelixCollectionResponse<Helix.User.User>>(
                    It.IsAny<UrlFragment>(),
                    It.IsAny<IDictionary<Header, string>>(),
                    It.IsAny<IEnumerable<KeyValuePair<QueryParameter, string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(userResponse));
            _apiCore.Setup(x => x.GetAsync<HelixCollectionResponse<CustomReward>>(
                    It.IsAny<UrlFragment>(),
                    It.IsAny<IDictionary<Header, string>>(),
                    It.IsAny<IDictionary<QueryParameter, string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(customRewardResponse));

            await _rewardService.SynchronizeRewardsAsync(user.Id);
            var rewardDto = await _rewardService.FindRewardByIdAsync(rewardId);

            Assert.NotNull(rewardDto);
            Assert.Equal(rewardDto.Id.ToString(), customReward.Id);
            Assert.Equal(rewardDto.Title, customReward.Title);
            Assert.Equal(rewardDto.Prompt, customReward.Prompt);
            Assert.Equal(rewardDto.StreamerId, customReward.BroadcasterUserId);
            Assert.Equal(rewardDto.BackgroundColor, customReward.BackgroundColor);
            Assert.Equal(rewardDto.Speech.Enabled, customReward.IsUserInputRequired);
        }

        private async Task SetupDataAsync(Guid id)
        {
            var reward = new Entities.Reward
            {
                Id = id.ToString(),
                ImageUrl = null,
                Title = "Title",
                Prompt = "Prompt",
                Speech = new Speech(),
                StreamerId = id.ToString(),
                BackgroundColor = "#6441A4",
            };
            reward.AddAsset(FileName.FromString("file.mp3"), 100);
            await _rewardRepository.AddAsync(reward);
        }
    }
}
