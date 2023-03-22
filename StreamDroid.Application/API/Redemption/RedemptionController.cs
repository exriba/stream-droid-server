using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamDroid.Domain.Services.Redemption;

namespace StreamDroid.Application.API.Redemption
{
    /// <summary>
    /// Redemption controller.
    /// </summary>
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

        /// <summary>
        /// Finds redemption statistics by user id.
        /// </summary>
        /// <returns>A collection of reward redemption DTOs as an Ok HTTP response.</returns>
        [HttpGet]
        public async Task<IActionResult> FindRedemptionStatisticsByUserIdAsync()
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));
            var rewardRedemptions = await _redemptionService.FindRedemptionStatisticsByUserIdAsync(claim.Value);
            return Ok(rewardRedemptions);
        }

        /// <summary>
        /// Finds redemption statistics by reward id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <returns>A collection of user redemption DTOs as an Ok HTTP response.</returns>
        [HttpGet("reward/{rewardId}")]
        public async Task<IActionResult> FindRedemptionStatisticsByRewardIdAsync([FromRoute] Guid rewardId)
        {
            var userRedemptions = await _redemptionService.FindRedemptionStatisticsByRewardIdAsync(rewardId);
            return Ok(userRedemptions);
        }
    }
}
