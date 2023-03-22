using Ardalis.GuardClauses;
using StreamDroid.Core.Enums;

namespace StreamDroid.Core.ValueObjects
{
    /// <summary>
    /// FileName value object.
    /// </summary>
    public sealed class FileName : ValueObject
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; private init; }
        
        /// <summary>
        /// Media extension
        /// </summary>
        public MediaExtension MediaExtension { get; private init; }

        public FileName(string name, MediaExtension mediaExtension)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));
            Guard.Against.Null(mediaExtension, nameof(mediaExtension));

            Name = name.Trim();
            MediaExtension = mediaExtension;
        }

        /// <summary>
        /// Creates a FileName from the given string.
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <returns>A FileName</returns>
        /// <exception cref="ArgumentNullException">If the fileName is null</exception>
        /// <exception cref="ArgumentException">If the fileName is an empty or whitespace string. In addition, this exception will be thrown if the argument is not formatted properly as a file name</exception>
        public static FileName FromString(string fileName)
        {
            Guard.Against.NullOrWhiteSpace(fileName, nameof(fileName));

            var fileExtension = Path.GetExtension(fileName).Substring(1);
            var mediaExtension = MediaExtension.FromName(fileExtension.ToUpper());
            var filename = Path.GetFileNameWithoutExtension(fileName);
            Guard.Against.NullOrWhiteSpace(filename, nameof(filename));
            return new FileName(filename, mediaExtension);
        }

        public override string ToString() => $"{Name}{MediaExtension}";

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return MediaExtension;
        }
    }
}
