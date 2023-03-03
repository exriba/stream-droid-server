using Microsoft.Extensions.Options;
using StreamDroid.Infrastructure;
using StreamDroid.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using StreamDroid.Shared;
using StreamDroid.Domain.Settings;
using StreamDroid.Application.Services;
using StreamDroid.Application.Middleware;
using StreamDroid.Application.API.Converters;
using StreamDroid.Application.Settings;
using SharpTwitch.EventSub;
using SharpTwitch.Core;
using StreamDroid.Application;

var builder = WebApplication.CreateBuilder(args);

// Add Shared Configuration
builder.Configuration.Configure();

// Add Configuration Options
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.Key));

// Add Logging
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();
else
    builder.Logging.AddLog4Net();

// Add Services to the Container.
builder.Services.AddSingleton<IAppSettings>(options => options.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddHostedService<EventSubHostedService>();
builder.Services.AddTwitchCore(builder.Configuration);
builder.Services.AddTwitchEventSub();
builder.Services.AddDirectoryBrowser();
builder.Services.AddInfrastructureConfiguration(builder.Configuration);
builder.Services.AddServiceConfiguration();
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
    options.LogoutPath = "/logout";
    options.SlidingExpiration = true;
    options.Cookie.Name = "StreamDroid";
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
app.MapHub<AssetHub>("/hubs/events/{id}");
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseMiddleware<GlobalRequestHandler>();
app.Run();

