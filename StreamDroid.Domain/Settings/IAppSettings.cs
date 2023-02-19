namespace StreamDroid.Domain.Settings
{
    public interface IAppSettings
    {
        string ClientUri { get; set; }
        string StaticAssetPath { get; set; }
        string StaticAssetUri { get; set; }
    }
}
