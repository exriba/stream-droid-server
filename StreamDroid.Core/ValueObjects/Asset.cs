using Ardalis.GuardClauses;

namespace StreamDroid.Core.ValueObjects
{
    /// <summary>
    /// Asset value object.
    /// </summary>
    public sealed class Asset : ValueObject
    {
        /// <summary>
        /// Volume
        /// </summary>
        public int Volume { get; private init; }

        /// <summary>
        /// FileName
        /// </summary>
        public FileName FileName { get; private init; }

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
