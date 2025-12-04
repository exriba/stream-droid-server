using System.ComponentModel.DataAnnotations;

namespace StreamDroid.Application.API.Constraints
{
    /// <summary>
    /// File extension attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class FileExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public FileExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        /// <summary>
        /// Validates that each argument file contains a vakid extension.
        /// </summary>
        /// <param name="value">value</param>
        /// <returns><see langword="true"/> if the files contain valid extensions. Otherwise returns <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">If any file has an invalid extension.</exception>
        public override bool IsValid(object? value)
        {
            if (value is not IEnumerable<IFormFile> files)
            {
                return false;
            }

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
