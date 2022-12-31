using StreamDroid.Application.API.Converters;
using StreamDroid.Core.ValueObjects;
using System.Buffers;
using System.Text.Json;

namespace StreamDroid.Application.Tests.API.Converters
{
    public class RewardConverterTests
    {
        private readonly RewardConverter _rewardConverter;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public RewardConverterTests()
        {
            _rewardConverter = new RewardConverter();
            _jsonSerializerOptions = new JsonSerializerOptions();
        }

        [Fact]
        public void RewardConverter_Read_Throws()
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                var jsonReader = new Utf8JsonReader();
                _rewardConverter.Read(ref jsonReader, typeof(string), _jsonSerializerOptions);
            });
        }

        [Fact]
        public void RewardConverter_Write()
        {
            var reward = new Core.Entities.Reward
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Test",
                Prompt = "Test",
                Speech = new Speech(),
                ImageUrl = null,
                BackgroundColor = "#FFFFFF"
            };

            var bufferWriter = new ArrayBufferWriter<byte>();
            var writer = new Utf8JsonWriter(bufferWriter);

            _rewardConverter.Write(writer, reward, _jsonSerializerOptions);

            Assert.NotEqual(0, writer.BytesPending);
        }
    }
}
