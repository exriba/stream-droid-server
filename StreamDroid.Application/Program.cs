using SharpTwitch.Core.Configuration;
using StreamDroid.Application.Configuration;
using SharpTwitch.Core.Interfaces;
using Microsoft.Extensions.Options;
using StreamDroid.Infrastructure;
using StreamDroid.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using StreamDroid.Shared;
using StreamDroid.Domain.Configuration;
using StreamDroid.Application.Services;
using StreamDroid.Application.Middleware;
using StreamDroid.Application.API.Converters;
using StreamDroid.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.Configure();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.Key));
builder.Services.Configure<CoreSettings>(builder.Configuration.GetSection(CoreSettings.Key));
builder.Services.Configure<PersistenceSettings>(builder.Configuration.GetSection(PersistenceSettings.Key));
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();
else
    builder.Logging.AddLog4Net();

// Add services to the container.
builder.Services.AddControllers()
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.Converters.Add(new RewardConverter());
                    options.JsonSerializerOptions.Converters.Add(new AssetConverter());
                    options.JsonSerializerOptions.Converters.Add(new UserConverter());
                });
builder.Services.AddDirectoryBrowser();
builder.Services.AddSingleton<IPersistenceSettings>(options => options.GetRequiredService<IOptions<PersistenceSettings>>().Value);
builder.Services.AddSingleton<ICoreSettings>(options => options.GetRequiredService<IOptions<CoreSettings>>().Value);
builder.Services.AddSingleton<IAppSettings>(options => options.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddSingleton<IAppSettings>(options => options.GetRequiredService<IOptions<AppSettings>>().Value);
builder.Services.AddSingleton<TwitchPubSubClient>();
builder.Services.AddHostedService<TwitchPubSubHost>();
builder.Services.AddInfrastructureConfiguration();
builder.Services.AddServiceConfiguration();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
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
})
.AddCookie(options =>
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

