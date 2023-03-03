using Microsoft.Extensions.Configuration;
using StreamDroid.Shared;

namespace StreamDroid.Application.Tests.Common
{
    public abstract class TestFixture
    {
        protected TestFixture()
        {
            using var configurationManager = new ConfigurationManager();
            configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Common/appsettings.Test.json")
                .Build();
            configurationManager.Configure();
        }
    }
}
