using System.Text.Json;
using System.Text.Json.Serialization;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Application.API.Converters
{
    public sealed class RewardConverter : JsonConverter<Entities.Reward>
    {
        public override Entities.Reward? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Entities.Reward value, JsonSerializerOptions options)
        {
            var reward = new
            {
                value.Id,
                value.Title,
                value.Prompt,
                value.Speech,
                value.ImageUrl,
                value.BackgroundColor
            };

            var json = JsonSerializer.Serialize(reward);
            writer.WriteRawValue(json);
        }
    }
}
