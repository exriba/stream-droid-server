using StreamDroid.Infrastructure;
using StreamDroid.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using StreamDroid.Shared;
using StreamDroid.Application.Settings;
using StreamDroid.Application.Middleware;
using StreamDroid.Application.API.Converters;
using StreamDroid.Application;
using Microsoft.Extensions.Options;
using StreamDroid.Domain.Settings;
using SharpTwitch.EventSub;

#region Constants
const string LOGOUT_PATH = "/logout";
const string COOKIE_NAME = "StreamDroid";
const string LOG4NET_CONFIG = "log4net.config";
const string APP_SETTINGS_JSON = "appsettings.json";
#endregion

// StreamDroid.Application Configuration
var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();
else
{
    var pathToExe = Environment.ProcessPath;
    var pathToContentRoot = Path.GetDirectoryName(pathToExe);
    Directory.SetCurrentDirectory(pathToContentRoot);
    builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
    builder.Configuration.AddJsonFile(APP_SETTINGS_JSON, optional: false, reloadOnChange: true);
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
builder.Services.AddMvc(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});
#endregion

#region Configure HTTP pipeline.
var app = builder.Build();
// app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseLocalFileServer();
app.UseAuthorization();
app.MapControllers();
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseMiddleware<GlobalRequestHandler>();
#endregion

app.Run();