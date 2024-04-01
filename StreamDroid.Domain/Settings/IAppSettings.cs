namespace StreamDroid.Domain.Settings
{
    /// <summary>
    /// Defines the app settings.
    /// </summary>
    public interface IAppSettings
    {
        string StaticAssetUri { get; set; }

        string ApplicationName { get; set; }
    }
}
