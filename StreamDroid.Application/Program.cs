using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharpTwitch.EventSub;
using StreamDroid.Application;
using StreamDroid.Application.API.Converters;
using StreamDroid.Application.Middleware;
using StreamDroid.Application.Settings;
using StreamDroid.Domain;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure;
using StreamDroid.Shared;
using System.Text;

#region Constants
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
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.Key));
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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
{
    var jwtSettings = new JwtSettings();
    builder.Configuration.GetSection(JwtSettings.Key).Bind(jwtSettings);
    var encodedKey = Encoding.UTF8.GetBytes(jwtSettings.SigningKey);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(encodedKey),
        ClockSkew = TimeSpan.FromSeconds(0)
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