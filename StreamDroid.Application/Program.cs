using StreamDroid.Infrastructure;
using StreamDroid.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using StreamDroid.Shared;
using StreamDroid.Application.Settings;
using StreamDroid.Application.Middleware;
using StreamDroid.Application.API.Converters;
using SharpTwitch.EventSub;
using StreamDroid.Application;
using StreamDroid.Domain.Services.Stream;
using Microsoft.Extensions.Options;
using StreamDroid.Domain.Settings;

// StreamDroid.Application Configuration
var builder = WebApplication.CreateBuilder(args);

#region Constants
const string LOGOUT_PATH = "/logout";
const string COOKIE_NAME = "StreamDroid";
const string HUB_PATTERN = "/hubs/events/{id}";
#endregion

#region Logging
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();
else
    builder.Logging.AddLog4Net();
#endregion

#region Options
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.Key));
#endregion

#region Shared
builder.Configuration.Configure();
#endregion

#region Register Services
builder.Services.AddSingleton<IAppSettings>(options => options.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddInfrastructureConfiguration(builder.Configuration);
builder.Services.AddServiceConfiguration(builder.Configuration);
builder.Services.AddDirectoryBrowser();
builder.Services.AddTwitchEventSub();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new AssetConverter()));
builder.Services.AddCors(options =>
{
    var appSettings = new AppSettings();
    builder.Configuration.GetSection(AppSettings.Key).Bind(appSettings);
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(appSettings.ClientUri)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
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
        context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
        return Task.CompletedTask;
    };
});
#endregion

#region Configure HTTP pipeline.
var app = builder.Build();
// app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseStaticFileServer();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AssetHub>(HUB_PATTERN);
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseMiddleware<GlobalRequestHandler>();
#endregion

app.Run();