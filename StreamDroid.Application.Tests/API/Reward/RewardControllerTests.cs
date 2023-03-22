using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamDroid.Application.API.Reward;
using StreamDroid.Core.ValueObjects;
using System.Security.Claims;
using StreamDroid.Application.API.Models;
using StreamDroid.Domain.Services.Reward;
using StreamDroid.Domain.DTOs;

namespace StreamDroid.Application.Tests.API.Reward
{
    public class RewardControllerTests : IDisposable
    {
        private const string ID = "Id";

        private readonly RewardController _rewardController;
        private readonly Mock<IRewardService> _mockRewardService;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public RewardControllerTests()
        {
            var claims = new List<Claim> 
            {
                new Claim(ID, Guid.NewGuid().ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims);

            var id = Guid.NewGuid();
            var reward = CreateRewardDto(id);
            var rewards = new List<RewardDto> { reward };
            _mockRewardService = new Mock<IRewardService>();
            _mockRewardService.Setup(x => x.FindRewardByIdAsync(It.IsAny<Guid>())).ReturnsAsync(reward);
            _mockRewardService.Setup(x => x.FindRewardsByUserIdAsync(It.IsAny<string>())).ReturnsAsync(rewards);

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
            _mockRewardService.Setup(x => x.SynchronizeRewardsAsync(It.IsAny<string>()))
                              .Returns(Task.CompletedTask);

            var result = await _rewardController.SynchronizeRewardsAsync();

            Assert.Equal(typeof(OkResult), result.GetType());
        }

        [Fact]
        public async Task RewardController_FindRewardsByUserIdAsync()
        {
            var result = await _rewardController.FindRewardsByUserIdAsync();

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public async Task RewardController_FindRewardByIdAsync()
        {
            var result = await _rewardController.FindRewardByIdAsync(Guid.NewGuid());

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public async Task RewardController_FindAssetsByRewardIdAsync()
        {
            var id = Guid.NewGuid();

            var result = await _rewardController.FindAssetsByRewardIdAsync(id);

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public async Task RewardController_AddAssetsToRewardAsync()
        {
            var id = Guid.NewGuid();
            var tuple = CreateAssets(id);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(x => x.FileName).Returns(tuple.Item2.First().FileName.ToString());

            _mockRewardService.Setup(x => x.AddAssetsToRewardAsync(It.IsAny<Guid>(), It.IsAny<IDictionary<FileName, int>>()))
                              .ReturnsAsync(tuple);

            var result = await _rewardController.AddAssetsToRewardAsync(id, new AssetForm { Files = new IFormFile[] { mockFile.Object } });

            Assert.Equal(typeof(CreatedAtActionResult), result.GetType());

            var directoryPath = @$"{Directory.GetCurrentDirectory()}\{tuple.Item1}";
            var filePath = @$"{directoryPath}\{tuple.Item2.First().FileName}";
            Directory.Delete(directoryPath, true);

            Assert.False(File.Exists(filePath));
            Assert.False(Directory.Exists(directoryPath));
        }

        [Fact]
        public async Task RewardController_UpdateRewardSpeechAsync()
        {
            var id = Guid.NewGuid();
            var speech = new Speech();

            var result = await _rewardController.UpdateRewardSpeechAsync(id, speech);

            Assert.Equal(typeof(OkResult), result.GetType());
        }

        [Fact]
        public async Task RewardController_UpdateAssetsFromRewardAsync()
        {
            var id = Guid.NewGuid();
            var tuple = CreateAssets(id);
            var dictionary = new Dictionary<string, int>
            {
                { "file.mp4", 50 }
            };

            _mockRewardService.Setup(x => x.AddAssetsToRewardAsync(It.IsAny<Guid>(), It.IsAny<IDictionary<FileName, int>>()))
                              .ReturnsAsync(tuple);

            var result = await _rewardController.UpdateAssetsFromRewardAsync(id, dictionary);

            Assert.Equal(typeof(OkResult), result.GetType());
        }

        [Fact]
        public async Task RewardController_RemoveAssetsFromRewardAsync()
        {
            var id = Guid.NewGuid();
            var tuple = CreateAssets(id);

            var result = await _rewardController.RemoveAssetsFromRewardAsync(id, tuple.Item2.First().FileName.ToString());

            Assert.Equal(typeof(OkResult), result.GetType());
        }

        private static RewardDto CreateRewardDto(Guid id)
        {
            return new RewardDto
            {
                Id = id,
                Title = "Title",
                Prompt = "Prompt",
                ImageUrl = null,
                Speech = new Speech(),
                StreamerId = id.ToString(),
                BackgroundColor = "#6441A4"
            };
        }

        private static Tuple<string, IReadOnlyCollection<Asset>> CreateAssets(Guid id)
        {
            var reward = new Core.Entities.Reward { Id = id.ToString(), Title = "Title" };
            var asset = reward.AddAsset(FileName.FromString("file.mp4"), 100);
            return Tuple.Create<string, IReadOnlyCollection<Asset>>(reward.Title, new List<Asset> { asset });
        }

        public void Dispose()
        {
            _rewardController.Dispose();
        }
    }
}
