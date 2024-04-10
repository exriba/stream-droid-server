namespace StreamDroid.Domain.Settings
{
    /// <summary>
    /// Defines the app settings.
    /// </summary>
    public interface IAppSettings
    {
        string StaticAssetPath { get; set; }

        string ApplicationName { get; set; }
    }
}
