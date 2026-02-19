using Microsoft.Extensions.Configuration;

namespace StreamDroid.Shared.Tests.Common
{
    public sealed class TestFixture : IDisposable
    {
        private readonly ConfigurationManager _configurationManager;
        private const string FilePath = "Common/appsettings.Test.json";

        public TestFixture()
        {
            _configurationManager = new ConfigurationManager();
            _configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(FilePath)
                .Build();
            _configurationManager.Configure();
        }

        public void Dispose()
        {
            _configurationManager.Dispose();
        }
    }
}
