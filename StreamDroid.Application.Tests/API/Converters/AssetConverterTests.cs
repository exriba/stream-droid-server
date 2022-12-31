using StreamDroid.Application.API.Converters;
using StreamDroid.Core.ValueObjects;
using System.Buffers;
using System.Text.Json;

namespace StreamDroid.Application.Tests.API.Converters
{
    public class AssetConverterTests
    {
        private readonly AssetConverter _assetConverter;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public AssetConverterTests()
        {
            _assetConverter = new AssetConverter();
            _jsonSerializerOptions = new JsonSerializerOptions();
        }

        [Fact]
        public void AssetConverter_Read_Throws()
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                var jsonReader = new Utf8JsonReader();
                _assetConverter.Read(ref jsonReader, typeof(string), _jsonSerializerOptions);
            });
        }

        [Fact]
        public void AssetConverter_Write()
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

            var asset = reward.AddAsset(FileName.FromString("file.mp4"), 100);

            var bufferWriter = new ArrayBufferWriter<byte>();
            var writer = new Utf8JsonWriter(bufferWriter);

            _assetConverter.Write(writer, asset, _jsonSerializerOptions);

            Assert.NotEqual(0, writer.BytesPending);
        }
    }
}
