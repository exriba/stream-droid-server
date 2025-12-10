using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamDroid.Application.API.Models;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Services.Data;
using StreamDroid.Domain.Services.Reward;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace StreamDroid.Application.API.Reward
{
    /// <summary>
    /// Reward controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("/rewards")]
    public class RewardController : Controller
    {
        private const string ID = "Id";
        private const string ASSET_NAME = "ASSET_NAME";

        private readonly IAssetFileService _dataService;
        private readonly IRewardService _rewardService;

        public RewardController(IRewardService rewardService,
                                IAssetFileService dataService)
        {
            _dataService = dataService;
            _rewardService = rewardService;
        }

        /// <summary>
        /// Finds a collection of rewards for the given user id.
        /// </summary>
        /// <returns>A collection of reward DTOs.</returns>
        [HttpGet]
        public async Task<IActionResult> FindRewardsByUserIdAsync()
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));
            var rewards = await _rewardService.FindRewardsByUserIdAsync(claim.Value);
            return Ok(rewards);
        }

        /// <summary>
        /// Finds a reward by the given user id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <returns>A reward DTO.</returns>
        [HttpGet("{rewardId}")]
        public async Task<IActionResult> FindRewardByIdAsync([FromRoute] Guid rewardId)
        {
            var reward = await _rewardService.FindRewardByIdAsync(rewardId);
            return Ok(reward);
        }

        /// <summary>
        /// Synchronizes external rewards for the given user.
        /// </summary>
        [HttpGet("sync")]
        public async Task<IActionResult> SynchronizeRewardsAsync()
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));
            await _rewardService.SynchronizeRewardsAsync(claim.Value);
            return Ok();
        }

        /// <summary>
        /// Updates the speech for the given reward.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="speech">speech</param>
        [HttpPut("{rewardId}/speech")]
        public async Task<IActionResult> UpdateRewardSpeechAsync([FromRoute] Guid rewardId,
                                                                 [FromBody][Required] Speech speech)
        {
            await _rewardService.UpdateRewardSpeechAsync(rewardId, speech);
            return Ok();
        }

        /// <summary>
        /// Finds a collection of assets by the given reward id.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <returns>A collection of assets.</returns>
        [HttpGet("{rewardId}/assets")]
        public async Task<IActionResult> FindAssetsByRewardIdAsync([FromRoute] Guid rewardId)
        {
            var assets = await _rewardService.FindAssetsByRewardIdAsync(rewardId);
            return Ok(assets);
        }

        /// <summary>
        /// Adds a collection of assets to the given reward.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="assetForm">asset form</param>
        /// <returns>Created response with the new assets.</returns>
        [HttpPost("{rewardId}/assets")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> AddAssetsToRewardAsync([FromRoute] Guid rewardId,
                                                                [FromForm][Required] AssetForm assetForm)
        {
            if (!assetForm.Files.Any())
                return NoContent();

            var claim = User.Claims.First(c => c.Type.Equals(ID));

            var fileMap = assetForm.Files.ToDictionary(x => FileName.FromString(x.FileName), _ => assetForm.Volume);
            var tuple = await _rewardService.AddAssetsToRewardAsync(rewardId, fileMap);
            await _dataService.AddAssetFilesAsync(claim.Value, tuple.Item1, assetForm.Files);
            return CreatedAtAction(nameof(AddAssetsToRewardAsync), tuple.Item2);
        }

        /// <summary>
        /// Updates assets for the given reward.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="payload">payload</param>
        [HttpPut("{rewardId}/assets")]
        public async Task<IActionResult> UpdateAssetsFromRewardAsync([FromRoute] Guid rewardId,
                                                                     [FromBody][Required] IDictionary<string, int> payload)
        {
            var fileMap = payload.ToDictionary(x => FileName.FromString(x.Key), x => x.Value);
            await _rewardService.RemoveAssetsFromRewardAsync(rewardId, fileMap.Keys);
            var tuple = await _rewardService.AddAssetsToRewardAsync(rewardId, fileMap);
            return Ok();
        }

        /// <summary>
        /// Removes an asset from the given reward.
        /// </summary>
        /// <param name="rewardId">reward id</param>
        /// <param name="assetName">asset name</param>
        [HttpDelete("{rewardId}/assets")]
        public async Task<IActionResult> RemoveAssetsFromRewardAsync([FromRoute] Guid rewardId,
                                                                     [FromHeader(Name = ASSET_NAME)] string assetName)
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));

            var decodedName = HttpUtility.UrlDecode(assetName);
            var fileName = new FileName[] { FileName.FromString(decodedName) };
            var reward = await _rewardService.FindRewardByIdAsync(rewardId);
            await _rewardService.RemoveAssetsFromRewardAsync(rewardId, fileName);
            _dataService.DeleteAssetFile(claim.Value, reward.Title, fileName.First());
            return Ok();
        }
    }
}
