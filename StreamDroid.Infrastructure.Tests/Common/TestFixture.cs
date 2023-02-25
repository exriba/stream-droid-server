using Moq;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Infrastructure.Settings;

namespace StreamDroid.Infrastructure.Tests.Common
{
    public abstract class TestFixture : IDisposable
    {
        private readonly string _filePath;
        protected readonly UberRepository _uberRepository;

        protected TestFixture()
        {
            _filePath = @$"{Directory.GetCurrentDirectory()}/test.db";
            var fileStream = new FileStream(_filePath, FileMode.Create);
            fileStream.Dispose();

            var persistenceSettings = new Mock<IPersistenceSettings>();
            persistenceSettings.Setup(x => x.ConnectionString).Returns($"Filename={_filePath}");
            _uberRepository = new UberRepository(persistenceSettings.Object);
        }

        public void Dispose()
        {
            _uberRepository.Dispose();
            File.Delete(_filePath);
        }
    }
}
