using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StreamDroid.Application.API.Redemption;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Services.Redemption;
using System.Security.Claims;

namespace StreamDroid.Application.Tests.API.Redemption
{
    public class RedemptionControllerTests : IDisposable
    {
        private const string ID = "Id";

        private readonly RedemptionController _redemptionController;
        private readonly Mock<IRedemptionService> _mockRedemptionService;

        public RedemptionControllerTests()
        {
            var claims = new List<Claim>
            {
                new Claim(ID, Guid.NewGuid().ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims);

            _mockRedemptionService = new Mock<IRedemptionService>();
            _redemptionController = new RedemptionController(_mockRedemptionService.Object)
            {
                ControllerContext = new ControllerContext()
            };
            _redemptionController.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(claimsIdentity)
            };
        }

        [Fact]
        public async Task RedemptionController_FindRedemptionStatisticsByStreamerIdAsync()
        {
            var rewardRedemptionDtos = new List<RewardRedemptionDto>
            {
                {
                    new RewardRedemptionDto
                    {
                        Name = "Name",
                        Fill = "#6441A4",
                        Value = 100
                    }
                }
            };

            _mockRedemptionService.Setup(x => x.FindRedemptionStatisticsByStreamerIdAsync(It.IsAny<string>()))
                .ReturnsAsync(rewardRedemptionDtos);

            var result = await _redemptionController.FindRedemptionStatisticsByStreamerIdAsync();

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }

        [Fact]
        public async Task RedemptionController_FindRedemptionStatisticsByRewardIdAsync()
        {
            var id = Guid.NewGuid();
            var userRedemptionDtos = new List<UserRedemptionDto>
            {
                {
                    new UserRedemptionDto
                    {
                        Id = 123,
                        Redeems = 1,
                        Percentage = 100,
                        UserName = "Test",
                    }
                }
            };

            _mockRedemptionService.Setup(x => x.FindRedemptionStatisticsByRewardIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(userRedemptionDtos);

            var result = await _redemptionController.FindRedemptionStatisticsByRewardIdAsync(id);

            Assert.Equal(typeof(OkObjectResult), result.GetType());
        }


        public void Dispose()
        {
            _redemptionController.Dispose();
        }
    }
}
