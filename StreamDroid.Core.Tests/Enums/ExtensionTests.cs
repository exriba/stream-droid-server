using StreamDroid.Core.Enums;

namespace StreamDroid.Core.Tests.Enums
{
    public class ExtensionTests
    {
        [Fact]
        public void Extension()
        {
            var values = Enum.GetValues(typeof(Extension));

            foreach(var value in values)
            {
                var extension = (Extension) value;
                var val = extension.GetExtension();
                Assert.Equal(val, $".{value?.ToString()?.ToLower()}");
            }
        }
    }
}