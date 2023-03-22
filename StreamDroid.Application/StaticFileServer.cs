using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using StreamDroid.Application.Settings;

namespace StreamDroid.Application
{
    /// <summary>
    /// Static File Server for local use. 
    /// </summary>
    /// TODO: Replace with Amazon S3 or other CDN solutions for Cloud.   
    internal static class StaticFileServer
    {
        /// <summary>
        /// Configure file server middleware.
        /// </summary>
        /// <param name="app">web application</param>
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
