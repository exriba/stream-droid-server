using Microsoft.Extensions.Configuration;
using StreamDroid.Shared;

namespace StreamDroid.Domain.Tests.Common
{
    public abstract class TestFixture : IDisposable
    {
        private readonly ConfigurationManager _configurationManager;
        protected TestFixture()
        {
            _configurationManager = new ConfigurationManager();
            _configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json")
                .Build();
            _configurationManager.Configure();
        }

        public void Dispose() => _configurationManager.Dispose();
    }
}
