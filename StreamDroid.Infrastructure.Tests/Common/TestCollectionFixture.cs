namespace StreamDroid.Infrastructure.Tests.Common
{
    [CollectionDefinition(Definition)]
    public class TestCollectionFixture : ICollectionFixture<TestFixture>
    {
        public const string Definition = "Infrastructure Context";

    }
}
