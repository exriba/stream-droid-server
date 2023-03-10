using StreamDroid.Infrastructure;
using StreamDroid.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using StreamDroid.Shared;
using StreamDroid.Application.Settings;
using StreamDroid.Application.Middleware;
using StreamDroid.Application.API.Converters;
using SharpTwitch.EventSub;
using SharpTwitch.Core;
using StreamDroid.Application;
using StreamDroid.Domain.Services.Stream;
using Microsoft.Extensions.Options;
using StreamDroid.Domain.Settings;

const string LOGOUT_PATH = "/logout";
const string COOKIE_NAME = "StreamDroid";
const string HUB_PATTERN = "/hubs/events/{id}";

var builder = WebApplication.CreateBuilder(args);

// Add Logging
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();
else
    builder.Logging.AddLog4Net();

// Add Shared Configuration
builder.Configuration.Configure();

// Add Configuration Options
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.Key));

// Add Services to the Container.
builder.Services.AddSingleton<IAppSettings>(options => options.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddInfrastructureConfiguration(builder.Configuration);
builder.Services.AddServiceConfiguration();
builder.Services.AddDirectoryBrowser();
builder.Services.AddTwitchCore(builder.Configuration);
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

// Build WebApplication
var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.Run();

