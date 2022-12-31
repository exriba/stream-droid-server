namespace StreamDroid.Application.Services
{
    public class TwitchPubSubHost : IHostedService
    {
        private readonly TwitchPubSubClient _twitchPubSubClient;

        public TwitchPubSubHost(TwitchPubSubClient twitchPubSubClient)
        {
            _twitchPubSubClient = twitchPubSubClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _twitchPubSubClient.Connect();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _twitchPubSubClient.Disconnect();
            return Task.CompletedTask;
        }
    }
}
