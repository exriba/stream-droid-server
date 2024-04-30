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

        private readonly ITwitchEventSub _twitchEventSub;

        public EventController(ITwitchEventSub twitchEventSub)
        {
            _twitchEventSub = twitchEventSub;
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

            Response.Headers.Add(HeaderNames.Connection, CONNECTION);
            Response.Headers.Add(HeaderNames.CacheControl, CACHE_CONTROL);
            Response.Headers.Add(HeaderNames.ContentType, EVENT_STREAM_CONTENT_TYPE);

            await _twitchEventSub.SubscribeAsync(claim.Value, notificationHandler: SerializeData);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000);
            }

            _twitchEventSub.UnsubscribeAsync(claim.Value);
        }
    }
}
