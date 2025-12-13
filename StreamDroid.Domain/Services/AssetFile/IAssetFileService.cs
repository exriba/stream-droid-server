using Google.Protobuf;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Domain.Services.AssetFile
{
    /// <summary>
    /// Defines business logic regarding asset file management.
    /// </summary>
    public interface IAssetFileService
    {
        /// <summary>
        /// Saves asset files for a given user id and reward name. 
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="rewardName">reward name</param>
        /// <param name="fileName">file name</param>
        /// <param name="byteString">byte string representation of the file</param>
        /// <param name="cancellationToken">cancellation token</param>
        Task AddAssetFileAsync(string userId, string rewardName, FileName fileName, ByteString byteString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an asset file from a given user id and reward name.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="rewardName">reward name</param>
        /// <param name="fileName">file name</param>
        void DeleteAssetFile(string userId, string rewardName, FileName fileName);
    }
}