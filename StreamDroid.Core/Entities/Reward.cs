using Ardalis.GuardClauses;
using StreamDroid.Core.Common;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Entities
{
    /// <summary>
    /// An entity that contains reward details. 
    /// </summary>
    public class Reward : EntityBase
    {
        /// <summary>
        /// Title or name
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Prompt message
        /// </summary>
        public string Prompt { get; set; } = "N/A";

        /// <summary>
        /// Background color
        /// </summary>
        public string BackgroundColor { get; set; } = string.Empty;

        /// <summary>
        /// ID of the user that owns this reward
        /// </summary>
        /// TODO: Consider replacing with 1 to 1 unidirectional relationship with User entity
        public string StreamerId { get; init; } = string.Empty;

        /// <summary>
        /// Speech value object
        /// </summary>
        public Speech Speech { get; set; } = new Speech();

        /// <summary>
        /// Reward redemptions
        /// </summary>
        public IReadOnlyCollection<Redemption> Redemptions { get; private init; } = new List<Redemption>();

        private string? _imageUrl;
        /// <summary>
        /// Image Url
        /// </summary>
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

        /// <summary>
        /// Reward assets
        /// </summary>
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

        /// <summary>
        /// Gets a random asset.
        /// </summary>
        /// <param name="asset">asset</param>
        /// <returns><see langword="true"/> and a random asset if the number of assets is greater than 0. Otherwise returns <see langword="false"/> and null.</returns>
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

        /// <summary>
        /// Gets an asset by name.
        /// </summary>
        /// <param name="assetName">asset name</param>
        /// <returns>The asset with the given name or null if not found.</returns>
        /// <exception cref="ArgumentNullException">If the asset name is null</exception>
        /// <exception cref="ArgumentException">If the asset name is empty or whitespace string</exception>
        public Asset? GetAsset(string assetName)
        {
            Guard.Against.NullOrWhiteSpace(assetName, nameof(assetName));
            return _assets.FirstOrDefault(asset => asset.ToString().Equals(assetName));
        }

        /// <summary>
        /// Adds an asset to the asset collection.
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="volume">volume</param>
        /// <returns>The new asset.</returns>
        /// <exception cref="DuplicateAssetException">If the asset already exists in the collection</exception>
        public Asset AddAsset(FileName fileName, int volume)
        {
            var asset = new Asset(fileName, volume);
            var added = _assets.Add(asset);
            return added ? asset : throw new DuplicateAssetException(fileName);
        }

        /// <summary>
        /// Remove an asset by name.
        /// </summary>
        /// <param name="assetName">asset name</param>
        /// <exception cref="ArgumentNullException">If the asset name is null</exception>
        /// <exception cref="ArgumentException">If the asset name is empty or whitespace string</exception>
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
