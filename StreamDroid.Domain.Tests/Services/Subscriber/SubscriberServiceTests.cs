using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Model;
using Microsoft.Extensions.Logging;
using Moq;
using StreamDroid.Domain.Notification;
using StreamDroid.Domain.Services.Stream;
using StreamDroid.Domain.Services.Subscriber;
using StreamDroid.Domain.Tests.Common;
using static Grpc.Model.NotificationEvent.Types;

namespace StreamDroid.Domain.Tests.Services.Subscriber
{
    [Collection(TestCollectionFixture.Definition)]
    public class SubscriberServiceTests
    {
        private readonly SubscriberService _subscriberService;
        private readonly NotificationRegistry _notificationRegistry;
        private readonly Mock<ITwitchSubscriber> _mockTwitchSubscriber;
        private readonly Func<CancellationTokenSource?, ServerCallContext> _createContext;

        public SubscriberServiceTests(TestFixture testFixture)
        {
            _notificationRegistry = new NotificationRegistry();
            _mockTwitchSubscriber = new Mock<ITwitchSubscriber>();
            var mockLogger = new Mock<ILogger<SubscriberService>>();
            _createContext = testFixture.createTestServerCallContext;

            _subscriberService = new SubscriberService(_mockTwitchSubscriber.Object, _notificationRegistry, mockLogger.Object);
        }

        [Fact]
        public async Task SubscriberService_Subscribe_Cancel()
        {
            var source = new CancellationTokenSource();
            var context = _createContext(source);

            var request = new Empty();
            var messages = new List<EventResponse>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            source.CancelAfter(100);

            await _subscriberService.Subscribe(request, mockStreamWriter.Object, context);

            source.Dispose();

            Assert.Empty(messages);
        }

        [Fact]
        public async Task SubscriberService_Subscribe()
        {
            var context = _createContext(null);

            var notificationEvent = new NotificationEvent
            {
                Id = Guid.NewGuid().ToString(),
                EventType = EventType.Audio,
                StreamerId = "userId",
                AssetFileEvent = new AssetFileEvent
                {
                    Uri = "uri",
                    Volume = 100
                }
            };

            var request = new Empty();
            var messages = new List<EventResponse>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            _ = _subscriberService.Subscribe(request, mockStreamWriter.Object, context);

            await _notificationRegistry.Publish("userId", notificationEvent);

            await Task.Delay(100);

            Assert.Single(messages);
        }

        #region Helpers
        private static Mock<IServerStreamWriter<EventResponse>> CreateServerStreamWriterMock(List<EventResponse> messages)
        {
            var mockStreamWriter = new Mock<IServerStreamWriter<EventResponse>>();
            mockStreamWriter.Setup(x => x.WriteAsync(
                It.IsAny<EventResponse>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((EventResponse eventResponse, CancellationToken token) => messages.Add(eventResponse));
            return mockStreamWriter;
        }
        #endregion
    }
}
