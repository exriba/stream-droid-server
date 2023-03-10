using Ardalis.GuardClauses;
using StreamDroid.Core.Common;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Entities
{
    public partial class Reward : EntityBase
    {
        public string Title { get; set; } = string.Empty;

        public string Prompt { get; set; } = "N/A";

        public string BackgroundColor { get; set; } = string.Empty;

        public string StreamerId { get; init; } = string.Empty;

        public Speech Speech { get; set; } = new Speech();

        public IReadOnlyCollection<Redemption> Redemptions { get; private init; } = new List<Redemption>();

        private string? _imageUrl;
        public string? ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (value is not null && !Uri.TryCreate(value, UriKind.Absolute, out _))
                    throw new ArgumentException("Invalid image url.", nameof(value));
                _imageUrl = value;
            }
        }

        private ISet<Asset> _assets = new HashSet<Asset>();
        public IReadOnlySet<Asset> Assets
        {
            get => _assets.ToHashSet();
            private set
            {
                if (value is null)
                    return;
                if (_assets.Count is 0)
                    _assets = value.ToHashSet();
            }
        }

        public void EnableTextToSpeech()
        {
            Speech = new Speech(true, Speech.VoiceIndex);
        }

        public void DisableTextToSpeech()
        {
            Speech = new Speech(false, Speech.VoiceIndex);
        }

        public bool TryGetRandomAsset(out Asset? asset)
        {
            asset = null;
            if (_assets.Count is 0)
                return false;

            var random = new Random();
            var index = random.Next(_assets.Count);
            asset = _assets.ToList()[index];
            return true;
        }

        public Asset? GetAsset(string assetName)
        {
            Guard.Against.NullOrWhiteSpace(assetName, nameof(assetName));
            return _assets.FirstOrDefault(asset => asset.ToString().Equals(assetName));
        }

        public Asset AddAsset(FileName fileName, int volume)
        {
            var asset = new Asset(fileName, volume);
            var added = _assets.Add(asset);
            return added ? asset : throw new DuplicateAssetException(this, fileName);
        }

        public void RemoveAsset(string assetName)
        {
            Guard.Against.NullOrWhiteSpace(assetName, nameof(assetName));
            var asset = _assets.FirstOrDefault(asset => asset.ToString().Equals(assetName));
            _assets.Remove(asset);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType()) return false;
            if (ReferenceEquals(this, obj)) return true;
            var that = obj as Reward;
            return Id.Equals(that.Id);
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
