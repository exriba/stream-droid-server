using Microsoft.Extensions.Configuration;
using StreamDroid.Shared;

namespace StreamDroid.Core.Tests.Common
{
    public sealed class TestFixture
    {
        public TestFixture()
        {
            var dictionary = new Dictionary<string, string>
            {
                { "EncryptionSettings:KeyPhrase", "w9z$C&F)H@McQfTj" },
                { "EncryptionSettings:Salt", "6eb7dedd-d9b0-466c-b1a7-36c7fee348b5" }
            };

            using var configurationManager = new ConfigurationManager();
            configurationManager.AddInMemoryCollection(dictionary!).Build();
            configurationManager.Configure();
        }
    }
}
