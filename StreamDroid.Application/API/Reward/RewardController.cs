using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Application.Helpers;
using StreamDroid.Domain.Reward;
using StreamDroid.Application.API.Models;
using System.Web;

namespace StreamDroid.Application.API.Reward
{
    [Authorize]
    [ApiController]
    [Route("/rewards")]
    public class RewardController : Controller
    {
        private const string ASSET_NAME = "ASSET_NAME";
        private readonly IRewardService _rewardService;
        private readonly IWebHostEnvironment _environment;

        public RewardController(IRewardService rewardService, IWebHostEnvironment environment)
        {
            _environment = environment;
            _rewardService = rewardService;
        }

        [HttpGet("sync")]
        public async Task<IActionResult> SyncRewards()
        {
            var claim = User.Claims.First(c => c.Type.Equals(Constants.ID));
            await _rewardService.SyncRewards(claim.Value);
            return Ok();
        }

        [HttpGet]
        public IActionResult Index()
        {
            var claim = User.Claims.First(c => c.Type.Equals(Constants.ID));
            var rewards = _rewardService.FindRewardsByUserId(claim.Value);
            return Ok(rewards);
        }

        [AllowAnonymous]
        [HttpGet("{rewardId}")]
        public IActionResult GetReward([FromRoute] Guid rewardId)
        {
            var id = rewardId.ToString();
            var reward = _rewardService.FindRewardById(id);
            return Ok(reward);
        }

        [HttpGet("{rewardId}/assets")]
        public IActionResult GetRewardAssets([FromRoute] Guid rewardId)
        {
            var id = rewardId.ToString();
            var reward = _rewardService.FindRewardById(id);
            return Ok(reward.Assets);
        }

        [HttpPost("{rewardId}/assets")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> AddAssets([FromRoute] Guid rewardId,
                                                   [FromForm][Required] AssetForm assetForm)
        {
            if (!assetForm.Files.Any())
                return NoContent();

            var id = rewardId.ToString();
            var fileMap = assetForm.Files.ToDictionary(x => FileName.FromString(x.FileName), _ => assetForm.Volume);
            var tuple = _rewardService.AddRewardAssets(id, fileMap);
            await SaveAssetFiles(tuple.Item1, assetForm.Files);
            return CreatedAtAction(nameof(AddAssets), tuple.Item2);
        }

        [HttpPut("{rewardId}/speech")]
        public IActionResult UpdateSpeech([FromRoute] Guid rewardId,
                                          [FromBody][Required] Speech speech)
        {
            var id = rewardId.ToString();
            var reward = _rewardService.UpdateRewardSpeech(id, speech);
            return Ok(reward);
        }

        [HttpPut("{rewardId}/assets")]
        public IActionResult UpdateAssets([FromRoute] Guid rewardId,
                                          [FromBody][Required] IDictionary<string, int> payload)
        {
            var id = rewardId.ToString();
            var fileMap = payload.ToDictionary(x => FileName.FromString(x.Key), x => x.Value);
            _rewardService.RemoveRewardAssets(id, fileMap.Keys);
            var tuple = _rewardService.AddRewardAssets(id, fileMap);
            return Ok();
        }

        [HttpDelete("{rewardId}/assets")]
        public IActionResult DeleteAsset([FromRoute] Guid rewardId,
                                         [FromHeader(Name = ASSET_NAME)] string assetName)
        {
            var id = rewardId.ToString();
            var decodedName = HttpUtility.UrlDecode(assetName);
            var fileName = FileName.FromString(decodedName);
            var fileNames = new FileName[] { fileName };
            var reward = _rewardService.FindRewardById(id);
            _rewardService.RemoveRewardAssets(id, fileNames);
            DeleteAssetFile(reward.Title, fileName);
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
