using Ardalis.GuardClauses;
using StreamDroid.Core.Enums;

namespace StreamDroid.Core.ValueObjects
{
    public sealed class FileName : ValueObject
    {
        public string Name { get; private set; }
        public Extension Extension { get; private set; }

        public FileName(string name, Extension extension)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));

            Name = name.Trim();
            Extension = extension;
        }

        public static FileName FromString(string fileName)
        {
            Guard.Against.NullOrWhiteSpace(fileName, nameof(fileName));

            var fileExtension = Path.GetExtension(fileName);
            Guard.Against.NullOrWhiteSpace(fileExtension, nameof(fileExtension));

            var filename = Path.GetFileNameWithoutExtension(fileName);
            Guard.Against.NullOrWhiteSpace(filename, nameof(filename));

            if (!Enum.TryParse(fileExtension[1..].ToUpper(), out Extension extension))
                throw new ArgumentException("Invalid file extension.");

            return new FileName(filename, extension);
        }

        public override string ToString() => $"{Name}{Extension.GetExtension()}";

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Extension;
        }
    }
}
