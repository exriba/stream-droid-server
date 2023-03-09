using Ardalis.GuardClauses;
using StreamDroid.Core.Enums;

namespace StreamDroid.Core.ValueObjects
{
    public sealed class FileName : ValueObject
    {
        public string Name { get; private init; }
        public MediaExtension MediaExtension { get; private init; }

        public FileName(string name, MediaExtension mediaExtension)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));
            Guard.Against.Null(mediaExtension, nameof(mediaExtension));

            Name = name.Trim();
            MediaExtension = mediaExtension;
        }

        public static FileName FromString(string fileName)
        {
            Guard.Against.NullOrWhiteSpace(fileName, nameof(fileName));

            var fileExtension = Path.GetExtension(fileName);
            var mediaExtension = MediaExtension.FromName(fileExtension);
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
