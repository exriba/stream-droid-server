namespace StreamDroid.Domain.Configuration
{
    public interface IAppSettings
    {
        string ClientUri { get; set; }
        string StaticAssetPath { get; set; }
        string StaticAssetUri { get; set; }
    }
}
