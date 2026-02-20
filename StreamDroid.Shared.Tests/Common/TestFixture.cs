using Microsoft.Extensions.Configuration;

namespace StreamDroid.Shared.Tests.Common
{
    public sealed class TestFixture
    {
        public TestFixture()
        {
            var dictionary = new Dictionary<string, string>
            {
                { "EncryptionSettings:KeyPhrase", "w9z$C&F)H@McQfTj" }
            };

            using var configurationManager = new ConfigurationManager();
            configurationManager.AddInMemoryCollection(dictionary).Build();
            configurationManager.Configure();
        }
    }
}
