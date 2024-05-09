using Microsoft.Extensions.Hosting;
using System.Net.NetworkInformation;

namespace StreamDroid.Domain.Services.Stream
{
    internal class TwitchHostedService : IHostedService
    {
        private readonly ITwitchManager _twitchManager;
        private volatile bool NetworkIsAvailable = false;

        public TwitchHostedService(ITwitchManager twitchManager)
        {
            _twitchManager = twitchManager;
        }

        private void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
            => NetworkIsAvailable = e.IsAvailable;

        ///<Summary>
        /// Initializes event sub client with twitch.
        /// Includes workaround for <see cref="TwitchHostedService.StopAsync(CancellationToken)">StopAsync</see>
        ///</Summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            NetworkIsAvailable = NetworkInterface.GetIsNetworkAvailable();
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (NetworkIsAvailable)
                {
                    await _twitchManager.ConnectAsync(); 
                    break;
                }

                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Clears all active eventsub subscriptions, then disconnects and disposes client. 
        /// Due to the following open <see href="https://github.com/dotnet/runtime/issues/83093">issue</see>, Windows Services do not exit/shutdown
        /// gracefully and thus this method is never called. As a result, StartAsync will handle clean up by removing inactive subscriptions from previous sessions. 
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Task</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _twitchManager.DisconnectAsync();
        }
    }
}
