using System.Text.Json;
using System.Text.Json.Serialization;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Application.API.Converters
{
    /// <summary>
    /// Asset JSON Converter
    /// </summary>
    public sealed class AssetConverter : JsonConverter<Asset>
    {
        public override Asset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes an asset as a JSON string to the output.
        /// </summary>
        /// <param name="writer">utf8 JSON writer</param>
        /// <param name="value">asset</param>
        /// <param name="options">JSON serializer options</param>
        public override void Write(Utf8JsonWriter writer, Asset value, JsonSerializerOptions options)
        {
            var asset = new
            {
                id = Guid.NewGuid().ToString(),
                fileName = value.ToString(),
                volume = value.Volume,
            };

            var json = JsonSerializer.Serialize(asset);
            writer.WriteRawValue(json);
        }
    }
}
