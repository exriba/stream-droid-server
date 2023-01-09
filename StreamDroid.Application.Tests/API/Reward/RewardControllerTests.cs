using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamDroid.Application.API.Reward;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Reward;
using System.Security.Claims;
using StreamDroid.Application.API.Models;

namespace StreamDroid.Application.Tests.API.Reward
{
    public class RewardControllerTests
    {
        private readonly RewardController _rewardController;
        private readonly Mock<IRewardService> _mockRewardService;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public RewardControllerTests()
        {
            var claims = new List<Claim> 
            {
                new Claim("Id", Guid.NewGuid().ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims);

            _mockRewardService = new Mock<IRewardService>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(x => x.WebRootPath).Returns(".");
            _rewardController = new RewardController(_mockRewardService.Object, _mockEnvironment.Object)
            {
                ControllerContext = new ControllerContext()
            };
            _rewardController.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(claimsIdentity)
            };
        }

        [Fact]
        public async Task RewardController_SyncRewards()
        {
            _mockRewardService.Setup(x => x.SyncRewards(It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _rewardController.SyncRewards();

            Assert.Equal(typeof(OkResult), result.GetType());
        }

        [Fact]
        public void RewardController_Index()
        {
            var id = Guid.NewGuid();
            var reward = CreateReward(id);
            var rewards = new List<Core.Entities.Reward> { reward };
            _mockRewardService.Setup(x => x.FindRewardsByUserId(It.IsAny<string>())).Returns(rewards);

            var result = _rewardController.Index();

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public void RewardController_GetReward()
        {
            var id = Guid.NewGuid();
            var reward = CreateReward(id);
            _mockRewardService.Setup(x => x.FindRewardById(It.IsAny<string>())).Returns(reward);

            var result = _rewardController.GetReward(Guid.NewGuid());

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public void RewardController_GetRewardAssets()
        {
            var id = Guid.NewGuid();
            var reward = CreateReward(id);
            reward.AddAsset(FileName.FromString("file.mp4"), 100);
            _mockRewardService.Setup(x => x.FindRewardById(It.IsAny<string>())).Returns(reward);

            var result = _rewardController.GetRewardAssets(id);

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public async Task RewardController_AddAssets()
        {
            var id = Guid.NewGuid();
            var tuple = CreateAssets(id);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(x => x.FileName).Returns(tuple.Item2.First().FileName.ToString());
            _mockRewardService.Setup(x => x.AddRewardAssets(It.IsAny<string>(), It.IsAny<IDictionary<FileName, int>>())).Returns(tuple);

            var result = await _rewardController.AddAssets(id, new AssetForm { Files = new IFormFile[] { mockFile.Object } });

            Assert.Equal(typeof(CreatedAtActionResult), result.GetType());

            var directoryPath = @$"{Directory.GetCurrentDirectory()}\{tuple.Item1}";
            var filePath = @$"{directoryPath}\{tuple.Item2.First().FileName}";
            Directory.Delete(directoryPath, true);
            Assert.False(File.Exists(filePath));
            Assert.False(Directory.Exists(directoryPath));
        }

        [Fact]
        public void RewardController_UpdateSpeech()
        {
            var id = Guid.NewGuid();
            var reward = CreateReward(id);
            var speech = new Speech();
            _mockRewardService.Setup(x => x.UpdateRewardSpeech(It.IsAny<string>(), It.IsAny<Speech>())).Returns(reward);

            var result = _rewardController.UpdateSpeech(id, speech);

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public void RewardController_UpdateAssets()
        {
            var id = Guid.NewGuid();
            var tuple = CreateAssets(id);
            var dictionary = new Dictionary<string, int>
            {
                { "file.mp4", 50 }
            };
            _mockRewardService.Setup(x => x.AddRewardAssets(It.IsAny<string>(), It.IsAny<IDictionary<FileName, int>>())).Returns(tuple);

            var result = _rewardController.UpdateAssets(id, dictionary);

            Assert.Equal(typeof(OkResult), result.GetType());
        }

        [Fact]
        public void RewardController_DeleteAsset()
        {
            var id = Guid.NewGuid();
            var reward = CreateReward(id);
            var tuple = CreateAssets(id);
            _mockRewardService.Setup(x => x.FindRewardById(It.IsAny<string>())).Returns(reward);

            var result = _rewardController.DeleteAsset(id, tuple.Item2.First().FileName.ToString());

            Assert.Equal(typeof(OkResult), result.GetType());
        }

        private static Core.Entities.Reward CreateReward(Guid id)
        {
            return new Core.Entities.Reward
            {
                Id = id.ToString(),
                Title = "Title"
            };
        }

        private static Tuple<string, IReadOnlyCollection<Asset>> CreateAssets(Guid id)
        {
            var reward = CreateReward(id);
            var asset = reward.AddAsset(FileName.FromString("file.mp4"), 100);
            return Tuple.Create<string, IReadOnlyCollection<Asset>>(reward.Title, new List<Asset> { asset });
        }
    }
}
