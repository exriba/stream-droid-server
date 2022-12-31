using Ardalis.GuardClauses;

namespace StreamDroid.Core.ValueObjects
{
    public sealed class Asset : ValueObject
    {
        public int Volume { get; private set; }
        public FileName FileName { get; private set; }

        private Asset() { }

        internal Asset(FileName fileName, int volume)
        {
            Guard.Against.AgainstExpression((volume) => volume >= 0 && volume <= 100, volume, "Value out of range.");
            
            Volume = volume;
            FileName = fileName;
        }

        public override string ToString() => FileName.ToString();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return FileName;
        }
    }
}
