using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Stream
{
    public interface ITwitchEventSub
    {
        Task ConnectAsync(Entities.User user);
        Task DisconnectAsync(Entities.User user);
    }
}
