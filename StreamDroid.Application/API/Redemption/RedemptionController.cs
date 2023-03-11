using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamDroid.Domain.Services.Redemption;

namespace StreamDroid.Application.API.Redemption
{
    [Authorize]
    [ApiController]
    [Route("/redemptions")]
    public class RedemptionController : Controller
    {
        private const string ID = "Id";

        private readonly IRedemptionService _redemptionService;

        public RedemptionController(IRedemptionService redemptionService)
        {
            _redemptionService = redemptionService;
        }

        [HttpGet]
        public async Task<IActionResult> FindRedemptionStatisticsByStreamerIdAsync()
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));
            var rewardRedemptions = await _redemptionService.FindRedemptionStatisticsByStreamerIdAsync(claim.Value);
            return Ok(rewardRedemptions);
        }

        [HttpGet("reward/{rewardId}")]
        public async Task<IActionResult> FindRedemptionStatisticsByRewardIdAsync([FromRoute] Guid rewardId)
        {
            var userRedemptions = await _redemptionService.FindRedemptionStatisticsByRewardIdAsync(rewardId);
            return Ok(userRedemptions);
        }
    }
}
