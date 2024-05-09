namespace StreamDroid.Domain.Services.Stream
{
    internal interface ITwitchManager
    {
        Task<bool> ConnectAsync();

        Task<bool> DisconnectAsync();
    }
}
