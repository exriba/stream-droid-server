using StreamDroid.Application.API.Converters;
using System.Buffers;
using System.Text.Json;

namespace StreamDroid.Application.Tests.API.Converters
{
    public class UserConverterTests
    {
        private readonly UserConverter _userConverter;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public UserConverterTests() 
        {
            _userConverter = new UserConverter();
            _jsonSerializerOptions = new JsonSerializerOptions();
        }

        [Fact]
        public void UserConverter_Read_Throws()
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                var jsonReader = new Utf8JsonReader();
                _userConverter.Read(ref jsonReader, typeof(string), _jsonSerializerOptions);
            });
        }

        [Fact]
        public void UserConverter_Write()
        {
            var user = new Core.Entities.User
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test",
                UserKey = Guid.NewGuid().ToString(),
            };

            var bufferWriter = new ArrayBufferWriter<byte>();
            var writer = new Utf8JsonWriter(bufferWriter);

            _userConverter.Write(writer, user, _jsonSerializerOptions);

            Assert.NotEqual(0, writer.BytesPending);
        }
    }
}
