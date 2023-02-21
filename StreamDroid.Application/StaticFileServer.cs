using Microsoft.Extensions.FileProviders;
using StreamDroid.Domain.Settings;

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
            var coreOptions = app.Services.GetRequiredService<IAppSettings>();

            app.UseFileServer(new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                RequestPath = coreOptions.StaticAssetPath,
                FileProvider = new PhysicalFileProvider(app.Environment.WebRootPath),
            });
        }
    }
}
