using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using static GrpcEventService;

namespace StreamDroid.Domain.Services.Stream
{
    [Authorize]
    public sealed class SubscriberService : GrpcEventServiceBase
    {
        private const string ID = "Id";

        private readonly ITwitchSubscriber _twitchSubscriber;
        private static readonly ConcurrentDictionary<string, Tuple<IServerStreamWriter<EventResponse>, CancellationToken>> _userStreamWriters = new();
        public static ConcurrentDictionary<string, Tuple<IServerStreamWriter<EventResponse>, CancellationToken>> UserStreamWriters => _userStreamWriters;

        public SubscriberService(ITwitchSubscriber twitchSubscriber)
        {
            _twitchSubscriber = twitchSubscriber;
        }

        public override async Task Subscribe(Empty request, IServerStreamWriter<EventResponse> responseStream, ServerCallContext context)
        {
            var userPrincipal = context.GetHttpContext().User;
            var claim = userPrincipal.Claims.First(c => c.Type.Equals(ID));

            var userId = claim.Value;
            var tuple = Tuple.Create(responseStream, context.CancellationToken);

            _userStreamWriters.TryAdd(userId, tuple);

            try
            {
                await _twitchSubscriber.SubscribeAsync(claim.Value, context.CancellationToken);
                await Task.Delay(Timeout.Infinite, context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                await _twitchSubscriber.UnsubscribeAsync(userId, CancellationToken.None);
            }
            finally
            {
                _userStreamWriters.TryRemove(userId, out _);
            }
        }
    }
}
