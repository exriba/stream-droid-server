namespace StreamDroid.Domain.Tests.Common
{
    [CollectionDefinition(Definition)]
    public class TestCollectionFixture : ICollectionFixture<TestFixture>
    {
        public const string Definition = "Domain Context";
    }
}
