using System.ComponentModel.DataAnnotations;

namespace StreamDroid.Application.API.Models
{
    /// <summary>
    /// Asset Form
    /// </summary>
    public class AssetForm
    {
        /// <summary>
        /// Volume
        /// </summary>
        public int Volume { get; set; } = 100;

        /// <summary>
        /// Files
        /// </summary>
        [Required]
        [Constraints.FileExtensions([".mp3", ".mp4"])]
        public required IEnumerable<IFormFile> Files { get; set; }
    }
}
