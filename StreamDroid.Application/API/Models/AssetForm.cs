using System.ComponentModel.DataAnnotations;

namespace StreamDroid.Application.API.Models
{
    public class AssetForm
    {
        public int Volume { get; set; } = 100;

        [Required]
        [Constraints.FileExtensions(new string[] { ".mp3", ".mp4" })]
        public IEnumerable<IFormFile> Files { get; set; }
    }
}
