using Microsoft.AspNetCore.Http;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Domain.Services.Data
{
    /// <summary>
    /// Defines business logic regarding data management.
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Saves asset files for a user id and reward name. 
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="rewardName">reward name</param>
        /// <param name="files">files</param>
        Task AddRewardAssetsAsync(string userId, string rewardName, IEnumerable<IFormFile> files);

        /// <summary>
        /// Deletes an asset file from a given user id and reward name.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="rewardName">reward name</param>
        /// <param name="fileName">file name</param>
        void DeleteRewardAsset(string userId, string rewardName, FileName fileName);
    }
}