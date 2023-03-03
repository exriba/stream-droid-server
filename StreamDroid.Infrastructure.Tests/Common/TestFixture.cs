using Microsoft.Extensions.Options;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Infrastructure.Settings;

namespace StreamDroid.Infrastructure.Tests.Common
{
    public abstract class TestFixture : IDisposable
    {
        private readonly string _filePath;
        protected readonly LiteDbUberRepository _uberRepository;

        protected TestFixture()
        {
            _filePath = @$"{Directory.GetCurrentDirectory()}/database.db";
            var fileStream = new FileStream(_filePath, FileMode.Create);
            fileStream.Dispose();

            var liteDbSettings = new LiteDbSettings(){ ConnectionString = $"Filename={_filePath}" };
            IOptions<LiteDbSettings> options = Options.Create(liteDbSettings);
            _uberRepository = new LiteDbUberRepository(options);
        }

        public void Dispose()
        {
            _uberRepository.Dispose();
            File.Delete(_filePath);
        }
    }
}
