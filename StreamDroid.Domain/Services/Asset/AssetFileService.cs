using Microsoft.AspNetCore.Http;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Settings;

namespace StreamDroid.Domain.Services.Data
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
        public async Task AddAssetFilesAsync(string userId, string rewardName, IEnumerable<IFormFile> files)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var basePath = Path.Combine(appDataPath, _appSettings.ApplicationName, _appSettings.StaticAssetPath, userId, rewardName);
            Directory.CreateDirectory(basePath);

            var tasks = new List<Task>();

            foreach (var file in files)
            {
                var filePath = Path.Combine(basePath, file.FileName);
                var stream = new FileStream(filePath, FileMode.Create);
                var task = file.CopyToAsync(stream).ContinueWith(async task =>
                {
                    await stream.DisposeAsync();
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        /// <inheritdoc/>
        public void DeleteAssetFile(string userId, string rewardName, FileName fileName)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var filePath = Path.Combine(appDataPath, _appSettings.ApplicationName, _appSettings.StaticAssetPath, userId, rewardName, fileName.ToString());

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
