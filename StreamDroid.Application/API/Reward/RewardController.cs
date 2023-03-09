using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Application.API.Models;
using StreamDroid.Domain.Services.Reward;
using System.Web;

namespace StreamDroid.Application.API.Reward
{
    [Authorize]
    [ApiController]
    [Route("/rewards")]
    public class RewardController : Controller
    {
        private const string ID = "Id";
        private const string ASSET_NAME = "ASSET_NAME";

        private readonly IRewardService _rewardService;
        private readonly IWebHostEnvironment _environment;

        public RewardController(IRewardService rewardService, 
                                IWebHostEnvironment environment)
        {
            _environment = environment;
            _rewardService = rewardService;
        }

        [HttpGet]
        public async Task<IActionResult> FindRewardsByStreamerIdAsync()
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));
            var rewards = await _rewardService.FindRewardsByStreamerIdAsync(claim.Value);
            return Ok(rewards);
        }

        [AllowAnonymous]
        [HttpGet("{rewardId}")]
        public async Task<IActionResult> FindRewardByIdAsync([FromRoute] Guid rewardId)
        {
            var reward = await _rewardService.FindRewardByIdAsync(rewardId);
            return Ok(reward);
        }

        [HttpGet("sync")]
        public async Task<IActionResult> SynchronizeRewardsAsync()
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));
            await _rewardService.SynchronizeRewardsAsync(claim.Value);
            return Ok();
        }

        [HttpPut("{rewardId}/speech")]
        public async Task<IActionResult> UpdateRewardSpeechAsync([FromRoute] Guid rewardId,
                                                                 [FromBody][Required] Speech speech)
        {
            await _rewardService.UpdateRewardSpeechAsync(rewardId, speech);
            return Ok();
        }

        [HttpGet("{rewardId}/assets")]
        public async Task<IActionResult> FindAssetsByRewardIdAsync([FromRoute] Guid rewardId)
        {
            var assets = await _rewardService.FindAssetsByRewardIdAsync(rewardId);
            return Ok(assets);
        }

        [HttpPost("{rewardId}/assets")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> AddAssetsToRewardAsync([FromRoute] Guid rewardId,
                                                                [FromForm][Required] AssetForm assetForm)
        {
            if (!assetForm.Files.Any())
                return NoContent();

            var fileMap = assetForm.Files.ToDictionary(x => FileName.FromString(x.FileName), _ => assetForm.Volume);
            var tuple = await _rewardService.AddAssetsToRewardAsync(rewardId, fileMap);
            await SaveAssetFiles(tuple.Item1, assetForm.Files);
            return CreatedAtAction(nameof(AddAssetsToRewardAsync), tuple.Item2);
        }

        [HttpPut("{rewardId}/assets")]
        public async Task<IActionResult> UpdateAssetsFromRewardAsync([FromRoute] Guid rewardId,
                                                                     [FromBody][Required] IDictionary<string, int> payload)
        {
            var fileMap = payload.ToDictionary(x => FileName.FromString(x.Key), x => x.Value);
            await _rewardService.RemoveAssetsFromRewardAsync(rewardId, fileMap.Keys);
            var tuple = await _rewardService.AddAssetsToRewardAsync(rewardId, fileMap);
            return Ok();
        }

        [HttpDelete("{rewardId}/assets")]
        public async Task<IActionResult> RemoveAssetsFromRewardAsync([FromRoute] Guid rewardId,
                                                                     [FromHeader(Name = ASSET_NAME)] string assetName)
        {
            var decodedName = HttpUtility.UrlDecode(assetName);
            var fileName = new FileName[] { FileName.FromString(decodedName) };
            var reward = await _rewardService.FindRewardByIdAsync(rewardId);
            await _rewardService.RemoveAssetsFromRewardAsync(rewardId, fileName);
            DeleteAssetFile(reward.Title, fileName.First());
            return Ok();
        }

        private async Task SaveAssetFiles(string rewardName, IEnumerable<IFormFile> files)
        {
            var basePath = Path.Combine(_environment.WebRootPath, rewardName);
            Directory.CreateDirectory(basePath);
            foreach (var file in files)
            {
                var filePath = Path.Combine(basePath, file.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
        }

        private void DeleteAssetFile(string rewardName, FileName fileName)
        {
            var basePath = Path.Combine(_environment.WebRootPath, rewardName);
            var filePath = Path.Combine(basePath, fileName.ToString());
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
