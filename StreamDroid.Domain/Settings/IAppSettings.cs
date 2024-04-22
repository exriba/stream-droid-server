namespace StreamDroid.Domain.Settings
{
    /// <summary>
    /// Defines the app settings.
    /// </summary>
    public interface IAppSettings
    {
        // TODO: review this field
        string ServerUri { get; set; }
        string StaticAssetPath { get; set; }
        string ApplicationName { get; set; }
    }
}
