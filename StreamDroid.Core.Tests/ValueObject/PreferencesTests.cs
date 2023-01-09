using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Tests.ValueObject
{
    public class PreferencesTests
    {
        [Fact]
        public void Equal()
        {
            var preferences = new Preferences();
            var preferences1 = new Preferences();      
            
            Assert.Equal(preferences, preferences1);
        }

        [Fact]
        public void NotEqual()
        {
            var preferences = new Preferences(50);
            var preferences1 = new Preferences();

            Assert.NotEqual(preferences, preferences1);
        }
    }
}
