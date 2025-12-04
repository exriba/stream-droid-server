using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using StreamDroid.Domain.Services.Stream;
using System.Text.Json;

namespace StreamDroid.Application.API.Event
{
    /// <summary>
    /// Event controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("/event")]
    public class EventController : Controller
    {
        private const string ID = "Id";
        private const string CONNECTION = "keep-alive";
        private const string CACHE_CONTROL = "no-cache";
        private const string EVENT_STREAM_CONTENT_TYPE = "text/event-stream";

        private readonly ITwitchSubscriber _twitchSubscriber;

        public EventController(ITwitchSubscriber twitchSubscriber)
        {
            _twitchSubscriber = twitchSubscriber;
        }

        [HttpGet]
        public async Task Connect(CancellationToken cancellationToken)
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));

            async Task SerializeData(dynamic sse)
            {
                await Response.WriteAsync("data: ");
                await JsonSerializer.SerializeAsync(Response.Body, sse);
                await Response.WriteAsync("\n");
                await Response.WriteAsync($"event: {sse.EventType}\n\n");
                await Response.Body.FlushAsync();
            }

            Response.Headers.Append(HeaderNames.Connection, CONNECTION);
            Response.Headers.Append(HeaderNames.CacheControl, CACHE_CONTROL);
            Response.Headers.Append(HeaderNames.ContentType, EVENT_STREAM_CONTENT_TYPE);

            await _twitchSubscriber.SubscribeAsync(claim.Value, notificationHandler: SerializeData);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000);
            }

            _twitchSubscriber.UnsubscribeAsync(claim.Value);
        }
    }
}
