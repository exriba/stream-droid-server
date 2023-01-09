using Moq;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Interfaces;
using SharpTwitch.Helix.Models;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Models;
using StreamDroid.Domain.Reward;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Domain.User;
using StreamDroid.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace StreamDroid.Domain.Tests.Reward
{
    public class RewardServiceTests : TestFixture
    {
        private readonly Core.Entities.Reward _reward;
        private readonly Mock<IApiCore> _apiCore;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<ICoreSettings> _coreSettings;
        private readonly Mock<IUberRepository> _uberRepository;

        public RewardServiceTests() : base()
        {
            _reward = CreateReward();
            _apiCore = new Mock<IApiCore>();
            _userService = new Mock<IUserService>();
            _coreSettings = new Mock<ICoreSettings>();
            _uberRepository = new Mock<IUberRepository>();

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
            var rewardService = new RewardService(_apiCore.Object, _userService.Object, _coreSettings.Object, _uberRepository.Object);
            Assert.ThrowsAny<ArgumentException>(() => rewardService.FindRewardById(id));
        }

        [Fact]
        public void RewardService_FindRewardById()
        {
            var rewardService = new RewardService(_apiCore.Object, _userService.Object, _coreSettings.Object, _uberRepository.Object);
            var entity = rewardService.FindRewardById(_reward.Id);

            Assert.Equal(_reward.Id, entity.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void RewardService_FindRewardsByUserId_Throws_InvalidArgs(string userId)
        {
            var rewardService = new RewardService(_apiCore.Object, _userService.Object, _coreSettings.Object, _uberRepository.Object);
            Assert.ThrowsAny<ArgumentException>(() => rewardService.FindRewardsByUserId(userId));
        }

        [Fact]
        public void RewardService_FindRewardsByUserId()
        {
            var rewardService = new RewardService(_apiCore.Object, _userService.Object, _coreSettings.Object, _uberRepository.Object);
            var rewards = rewardService.FindRewardsByUserId(_reward.StreamerId);

            Assert.NotEmpty(rewards);
        }

        [Fact]
        public void RewardService_UpdateRewardSpeech()
        {
            var speech = new Speech(true, 0);

            var rewardService = new RewardService(_apiCore.Object, _userService.Object, _coreSettings.Object, _uberRepository.Object);
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

            var rewardService = new RewardService(_apiCore.Object, _userService.Object, _coreSettings.Object, _uberRepository.Object);
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

            var rewardService = new RewardService(_apiCore.Object, _userService.Object, _coreSettings.Object, _uberRepository.Object);
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
            var user = new Core.Entities.User
            {
                Id = Guid.NewGuid().ToString(),
                AccessToken = "accessToken"
            };

            var channelPointReward = new ChannelPointReward
            {
                Id = Guid.NewGuid().ToString(),
                Image = new SharpTwitch.Helix.Models.Shared.Image
                {
                    Url1x = "http://localhost/image.png"
                },
                Title = _reward.Title,
                BroadcasterId = _reward.StreamerId,
                BackgroundColor = "#FFFFFF"
            };

            var helixResponse = new HelixCollectionResponse<ChannelPointReward>
            {
                Data = new List<ChannelPointReward> { channelPointReward }
            };

            static Task<string> refreshToken(Core.Entities.User user) => RefreshAccessToken(user);
            var tokenRefreshPolicy = new TokenRefreshPolicy(user, refreshToken);

            _userService.Setup(x => x.CreateTokenRefreshPolicy(It.IsAny<string>())).Returns(tokenRefreshPolicy);
            _apiCore.Setup(x =>x.GetAsync<HelixCollectionResponse<ChannelPointReward>>(
                    It.IsAny<UrlFragment>(), 
                    It.IsAny<IDictionary<Header, string>>(), 
                    It.IsAny<IDictionary<QueryParameter, string>>(), 
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(helixResponse));

            var rewardService = new RewardService(_apiCore.Object, _userService.Object, _coreSettings.Object, _uberRepository.Object);
            await rewardService.SyncRewards(user.Id);
            
            var reward = rewardService.FindRewardById(_reward.Id);

            Assert.NotNull(reward);
            Assert.Equal(reward.Title, channelPointReward.Title);
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

        private static Task<string> RefreshAccessToken(Core.Entities.User user)
        {
            return Task.FromResult(user.Id);
        }
    }
}
