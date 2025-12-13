using Google.Protobuf;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Settings;

namespace StreamDroid.Domain.Services.AssetFile
{
    /// <summary>
    /// Default implementation of <see cref="IAssetFileService"/>.
    /// </summary>
    internal sealed class AssetFileService : IAssetFileService
    {
        private readonly IAppSettings _appSettings;

        public AssetFileService(IAppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        /// <inheritdoc/>
        public async Task AddAssetFileAsync(string userId, string rewardName, FileName fileName, ByteString byteString, CancellationToken cancellationToken = default)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var basePath = Path.Combine(appDataPath, _appSettings.ApplicationName, _appSettings.StaticAssetPath, userId, rewardName);
            Directory.CreateDirectory(basePath);

            var filePath = Path.Combine(basePath, fileName.ToString());

            var bytes = byteString.ToByteArray();
            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
        }

        /// <inheritdoc/>
        public void DeleteAssetFile(string userId, string rewardName, FileName fileName)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var filePath = Path.Combine(appDataPath, _appSettings.ApplicationName, _appSettings.StaticAssetPath, userId, rewardName, fileName.ToString());

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
