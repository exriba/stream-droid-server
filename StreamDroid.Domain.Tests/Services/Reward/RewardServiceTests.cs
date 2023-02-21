using Moq;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Settings;
using SharpTwitch.Helix.Models;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Services.Reward;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Infrastructure.Persistence;
using System.Linq.Expressions;
using SharpTwitch.Core;
using SharpTwitch.Helix.Models.Shared;
using SharpTwitch.Helix;
using StreamDroid.Domain.RefreshPolicy;
using SharpTwitch.Helix.Models.Channel.Reward;

namespace StreamDroid.Domain.Tests.Services.Reward
{
    public class RewardServiceTests : TestFixture
    {
        private const string NEW_ACCESS_TOKEN = "NewAccessToken";

        private readonly HelixApi _helixApi;
        private readonly Mock<IApiCore> _apiCore;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IUberRepository> _uberRepository;
        private readonly Core.Entities.Reward _reward;

        public RewardServiceTests() : base()
        {
            _reward = CreateReward();
            _apiCore = new Mock<IApiCore>();
            _userService = new Mock<IUserService>();
            var coreSettings = new Mock<ICoreSettings>();
            _uberRepository = new Mock<IUberRepository>();
            _helixApi = new HelixApi(coreSettings.Object, _apiCore.Object);

            var rewards = new List<Core.Entities.Reward> { _reward };
            _uberRepository.Setup(x => x.Find(It.IsAny<Expression<Func<Core.Entities.Reward, bool>>>())).Returns(rewards);
            _uberRepository.Setup(x => x.Save(It.IsAny<Core.Entities.Reward>())).Returns(_reward);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void RewardService_FindRewardById_Throws_InvalidArgs(string id)
        {
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository.Object);
            
            Assert.ThrowsAny<ArgumentException>(() => rewardService.FindRewardById(id));
        }

        [Fact]
        public void RewardService_FindRewardById()
        {
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository.Object);
            var entity = rewardService.FindRewardById(_reward.Id);

            Assert.Equal(_reward.Id, entity.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void RewardService_FindRewardsByUserId_Throws_InvalidArgs(string userId)
        {
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository.Object);
            
            Assert.ThrowsAny<ArgumentException>(() => rewardService.FindRewardsByUserId(userId));
        }

        [Fact]
        public void RewardService_FindRewardsByUserId()
        {
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository.Object);
            var rewards = rewardService.FindRewardsByUserId(_reward.StreamerId);

            Assert.NotEmpty(rewards);
        }

        [Fact]
        public void RewardService_UpdateRewardSpeech()
        {
            var speech = new Speech(true, 0);
            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository.Object);
            var reward = rewardService.UpdateRewardSpeech(_reward.Id, speech);

            Assert.Equal(reward.Speech, speech);
        }

        [Fact]
        public void RewardService_AddRewardAssets()
        {
            var fileName = "file.mp4";
            var dictionary = new Dictionary<FileName, int>
            {
                { FileName.FromString(fileName), 100 },
            };

            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository.Object);
            var tuple = rewardService.AddRewardAssets(_reward.Id, dictionary);

            Assert.Equal(_reward.Title, tuple.Item1);
            Assert.NotEmpty(tuple.Item2);
        }

        [Fact]
        public void RewardService_RemoveRewardAssets()
        {
            var fileName = "file.mp4";
            var dictionary = new Dictionary<FileName, int>
            {
                { FileName.FromString(fileName), 100 },
            };

            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository.Object);
            rewardService.AddRewardAssets(_reward.Id, dictionary);
            var reward = rewardService.FindRewardById(_reward.Id);

            Assert.NotEmpty(reward.Assets);

            rewardService.RemoveRewardAssets(_reward.Id, dictionary.Keys);
            reward = rewardService.FindRewardById(_reward.Id);

            Assert.Empty(reward.Assets);
        }

        [Fact]
        public async Task RewardService_SyncRewards()
        {
            var accessToken = "accessToken";
            var userId = Guid.NewGuid().ToString();

            var user = new SharpTwitch.Helix.Models.User.User
            {
                Id = userId,
            };

            var userResponse = new HelixCollectionResponse<SharpTwitch.Helix.Models.User.User>
            {
                Data = new List<SharpTwitch.Helix.Models.User.User> { user }
            };

            var customReward = new CustomReward
            {
                Id = Guid.NewGuid().ToString(),
                Image = new Image
                {
                    Url1x = "http://localhost/image.png"
                },
                Title = _reward.Title,
                BroadcasterId = _reward.StreamerId,
                BackgroundColor = "#FFFFFF"
            };

            var customRewardResponse = new HelixCollectionResponse<CustomReward>
            {
                Data = new List<CustomReward> { customReward }
            };

            async Task<string> refreshToken(string userId) => await Task.FromResult(NEW_ACCESS_TOKEN);
            var tokenRefreshPolicy = new TokenRefreshPolicy(userId, accessToken, refreshToken);

            _userService.Setup(x => x.CreateTokenRefreshPolicy(It.IsAny<string>())).Returns(tokenRefreshPolicy);

            _apiCore.Setup(x => x.GetAsync<HelixCollectionResponse<SharpTwitch.Helix.Models.User.User>>(
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

            var rewardService = new RewardService(_helixApi, _userService.Object, _uberRepository.Object);
            await rewardService.SyncRewards(userId);

            var reward = rewardService.FindRewardById(_reward.Id);

            Assert.NotNull(reward);
            Assert.Equal(reward.Title, customReward.Title);
        }

        private static Core.Entities.Reward CreateReward()
        {
            return new Core.Entities.Reward
            {
                Id = Guid.NewGuid().ToString(),
                StreamerId = Guid.NewGuid().ToString(),
                Title = "Title"
            };
        }
    }
}
