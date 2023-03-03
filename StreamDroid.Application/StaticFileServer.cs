using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using StreamDroid.Application.Settings;

namespace StreamDroid.Application
{
    /// <summary>
    /// Static File Server for local use. 
    /// Use Amazon S3 or other solutions for Cloud.   
    /// </summary>
    internal static class StaticFileServer
    {
        public static void UseStaticFileServer(this WebApplication app)
        {
            var options = app.Services.GetRequiredService<IOptions<AppSettings>>();
            var appSettings = options.Value;

            app.UseFileServer(new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                RequestPath = appSettings.StaticAssetPath,
                FileProvider = new PhysicalFileProvider(app.Environment.WebRootPath),
            });
        }
    }
}
