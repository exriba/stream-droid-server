using System.ComponentModel.DataAnnotations;

namespace StreamDroid.Application.API.Constraints
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class FileExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public FileExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        public override bool IsValid(object? value)
        {
            var files = value as IEnumerable<IFormFile>;

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!_extensions.Contains(extension.ToLower()))
                    throw new ArgumentException("Invalid file extension.");
            }

            return true;
        }
    }
}
