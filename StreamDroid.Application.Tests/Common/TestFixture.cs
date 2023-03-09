using Microsoft.Extensions.Configuration;
using StreamDroid.Shared;

namespace StreamDroid.Application.Tests.Common
{
    public sealed class TestFixture
    {
        private const string FilePath = "Common/appsettings.Test.json";

        public TestFixture()
        {
            using var configurationManager = new ConfigurationManager();
            configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile(FilePath)
                                .Build();
            configurationManager.Configure();
        }
    }
}
