using System.Text.Json;
using System.Text.Json.Serialization;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Application.API.Converters
{
    public sealed class UserConverter : JsonConverter<Entities.User>
    {
        public override Entities.User? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Entities.User value, JsonSerializerOptions options)
        {
            var user = new
            {
                value.Id,
                value.Name,
                value.UserKey
            };

            var json = JsonSerializer.Serialize(user);
            writer.WriteRawValue(json);
        }
    }
}
