using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using SharpTwitch.EventSub;
using StreamDroid.Application;
using StreamDroid.Application.API.Converters;
using StreamDroid.Application.Middleware;
using StreamDroid.Application.Settings;
using StreamDroid.Domain;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure;
using StreamDroid.Shared;
using System.Net;

#region Constants
const string LOGOUT_PATH = "/logout";
const string COOKIE_NAME = "StreamDroid";
const string LOG4NET_CONFIG = "log4net.config";
#endregion

// StreamDroid.Application Configuration
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}
else
{
    builder.Logging.AddLog4Net(LOG4NET_CONFIG, watch: true);
}

#region Options
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.Key));
#endregion

#region Shared
builder.Configuration.Configure();
#endregion

#region Register Services

builder.Services.AddWindowsService();
builder.Services.AddSingleton<IAppSettings>(options => options.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddInfrastructureConfiguration(builder.Configuration);
builder.Services.AddServiceConfiguration(builder.Configuration);
builder.Services.AddDirectoryBrowser();
builder.Services.AddTwitchEventSub();
builder.Services.AddHttpClient();
builder.Services.AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new AssetConverter()));
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.LogoutPath = LOGOUT_PATH;
    options.SlidingExpiration = true;
    options.Cookie.Name = COOKIE_NAME;
    options.Events.OnRedirectToLogin = (context) =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return Task.CompletedTask;
    };
});
builder.Services.AddMvc(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});
#endregion

#region Configure HTTP pipeline.
var app = builder.Build();
// app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseLocalFileServer();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseMiddleware<GlobalRequestHandler>();
}
#endregion

await app.RunAsync();