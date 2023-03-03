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
using Helix = SharpTwitch.Helix.Models;

namespace StreamDroid.Domain.Tests.Services.Reward
{
    public class RewardServiceTests : TestFixture
    {
        private readonly HelixApi _helixApi;
        private readonly Mock<IApiCore> _apiCore;
        private readonly Mock<IUserService> _userService;

        public RewardServiceTests() : base("reward-database.db")
        {
            _apiCore = new Mock<IApiCore>();
            _userService = new Mock<IUserService>();
            var coreSettings = new Mock<ICoreSettings>();
            _helixApi = new HelixApi(coreSettings.Object, _apiCore.Object);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void RewardService_FindRewardById_Throws_InvalidArgs(string id)
        {
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);
            
            Assert.ThrowsAnyAsync<ArgumentException>(async() 
                => await rewardService.FindRewardById(id));
        }

        [Fact]
        public async Task RewardService_FindRewardById()
        {
            var reward = await CreateReward();
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);

            var rewardDto = await rewardService.FindRewardById(reward.Id);

            Assert.Equal(reward.Id, rewardDto.Id.ToString());
        }

        [Fact]
        public async Task RewardService_FindAssetsByRewardId()
        {
            var reward = await CreateReward();
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);

            var assets = await rewardService.FindAssetsByRewardId(reward.Id);

            Assert.NotEmpty(assets);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void RewardService_FindRewardsByUserId_Throws_InvalidArgs(string userId)
        {
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);
            
            Assert.ThrowsAnyAsync<ArgumentException>(async() 
                => await rewardService.FindRewardsByUserId(userId));
        }

        [Fact]
        public async Task RewardService_FindRewardsByUserId()
        {
            var reward = await CreateReward();
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);

            var rewards = await rewardService.FindRewardsByUserId(reward.StreamerId);

            Assert.NotEmpty(rewards);
        }

        [Fact]
        public async Task RewardService_UpdateRewardSpeech()
        {
            var speech = new Speech(true);
            var reward = await CreateReward();
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);

            await rewardService.UpdateRewardSpeech(reward.Id, speech);
            var rewardDto = await rewardService.FindRewardById(reward.Id);

            Assert.False(reward.Speech.Enabled);
            Assert.True(rewardDto.Speech.Enabled);
        }

        [Fact]
        public async Task RewardService_AddRewardAssets()
        {
            var fileName = "file.mp4";
            var reward = await CreateReward();
            var dictionary = new Dictionary<FileName, int>
            {
                { FileName.FromString(fileName), 100 },
            };
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);

            var tuple = await rewardService.AddRewardAssets(reward.Id, dictionary);

            Assert.Equal(reward.Title, tuple.Item1);
            Assert.NotEmpty(tuple.Item2);
        }

        [Fact]
        public async Task RewardService_RemoveRewardAssets()
        {
            var fileName = "file.mp4";
            var reward = await CreateReward();
            var dictionary = new Dictionary<FileName, int>
            {
                { FileName.FromString(fileName), 100 },
            };
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);

            await rewardService.AddRewardAssets(reward.Id, dictionary);
            var assets = await rewardService.FindAssetsByRewardId(reward.Id);

            Assert.Equal(2, assets.Count);

            await rewardService.RemoveRewardAssets(reward.Id, dictionary.Keys);
            assets = await rewardService.FindAssetsByRewardId(reward.Id);

            Assert.Equal(1, assets.Count);
        }

        [Fact]
        public async Task RewardService_SyncRewards()
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

            var customReward = new CustomReward
            {
                Id = Guid.NewGuid().ToString(),
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

            _userService.Setup(x => x.CreateTokenRefreshPolicy(It.IsAny<string>())).ReturnsAsync(tokenRefreshPolicy);

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

            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository);
            await rewardService.SyncRewards(user.Id);

            var rewardDto = await rewardService.FindRewardById(customReward.Id);

            Assert.NotNull(rewardDto);
            Assert.Equal(rewardDto.Id.ToString(), customReward.Id);
            Assert.Equal(rewardDto.Title, customReward.Title);
            Assert.Equal(rewardDto.Prompt, customReward.Prompt);
            Assert.Equal(rewardDto.StreamerId, customReward.BroadcasterUserId);
            Assert.Equal(rewardDto.BackgroundColor, customReward.BackgroundColor);
            Assert.Equal(rewardDto.Speech.Enabled, customReward.IsUserInputRequired);
        }

        private async Task<Core.Entities.Reward> CreateReward()
        {
            var reward = new Core.Entities.Reward
            {
                Id = Guid.NewGuid().ToString(),
                StreamerId = Guid.NewGuid().ToString(),
                Title = "Title",
                Prompt = "N/A",
                Speech = new Speech(),
                BackgroundColor = ""
            };
            reward.AddAsset(FileName.FromString("file.mp3"), 100);
            return await _uberRepository.Save(reward);
        }
    }
}
