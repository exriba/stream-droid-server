namespace StreamDroid.Application.Tests.Common
{
    [CollectionDefinition(Definition)]
    public class TestCollectionFixture : ICollectionFixture<TestFixture>
    {
        public const string Definition = "Application Context";
    }
}
