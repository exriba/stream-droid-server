using Ardalis.GuardClauses;
using StreamDroid.Core.Common;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Entities
{
    public partial class Reward : EntityBase
    {
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(Title));
                _title = value;
            }
        }

        private string _prompt = string.Empty;
        public string Prompt
        {
            get => _prompt;
            set
            {
                _prompt = value ?? "N/A";
            }
        }

        private string? _imageUrl;
        public string? ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (value != null && !Uri.TryCreate(value, UriKind.Absolute, out _))
                    throw new ArgumentException("Invalid image url.", nameof(value));
                _imageUrl = value;
            }
        }

        private string _streamerId = string.Empty;
        public string StreamerId
        {
            get => _streamerId;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(StreamerId));
                _streamerId = value;
            }
        }

        private string? _backgroundColor;
        public string? BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
            }
        }

        private Speech _speech = new Speech(false, 0);
        public Speech Speech
        {
            get => _speech;
            set
            {
                _speech = value ?? new Speech(false, 0);
            }
        }

        private ISet<Asset> _assets = new HashSet<Asset>();
        public IReadOnlyList<Asset> Assets
        {
            get => _assets.ToList();
            private set
            {
                if (value == null)
                    return;
                if (_assets.Count == 0)
                    _assets = value.ToHashSet();
            }
        }

        public void EnableTextToSpeech()
        {
            _speech = new Speech(true, _speech.VoiceIndex);
        }

        public void DisableTextToSpeech()
        {
            _speech = new Speech(false, _speech.VoiceIndex);
        }

        public Asset? GetRandomAsset()
        {
            if (_assets.Count == 0)
                return null;

            var random = new Random();
            var index = random.Next(_assets.Count);
            return _assets.ToList()[index];
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
            if (!added)
                throw new DuplicateAssetException(this, fileName);
            return asset;
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
