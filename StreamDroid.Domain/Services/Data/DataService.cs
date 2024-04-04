using Microsoft.AspNetCore.Http;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Settings;

namespace StreamDroid.Domain.Services.Data
{
    /// <summary>
    /// Default implementation of <see cref="IDataService"/>.
    /// </summary>
    public sealed class DataService : IDataService
    {
        private readonly IAppSettings _appSettings;

        public DataService(IAppSettings appSettings) 
        {
            _appSettings = appSettings;
        }

        /// <inheritdoc/>
        public async Task AddRewardAssetsAsync(string userId, string rewardName, IEnumerable<IFormFile> files)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var basePath = Path.Combine(appDataPath, _appSettings.ApplicationName, userId, rewardName);
            Directory.CreateDirectory(basePath);

            var tasks = new List<Task>();

            foreach (var file in files)
            {
                var filePath = Path.Combine(basePath, file.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                var task = file.CopyToAsync(stream);
                tasks.Add(task); 
            }

            await Task.WhenAll(tasks);
        }

        /// <inheritdoc/>
        public void DeleteRewardAsset(string userId, string rewardName, FileName fileName)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var filePath = Path.Combine(appDataPath, _appSettings.ApplicationName, userId, rewardName, fileName.ToString());

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
