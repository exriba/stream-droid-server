using StreamDroid.Core.Entities;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Core.Tests.Entities
{
    public class RewardTests
    {
        private const string TITLE = "Reward";
        private const string MP4FILE = "file.mp4";
        private const string DESCRIPTION = "Description";
        private const string BACKGROUND_COLOR = "#6441A4";

        [Theory]
        [InlineData(TITLE, DESCRIPTION, BACKGROUND_COLOR)]
        public void Reward_Created(string title, string prompt, string backgroundColor)
        {
            var speech = new Speech();
            var id = Guid.NewGuid().ToString();
            var streamerId = Guid.NewGuid().ToString();

            var reward = CreateReward(id, title, prompt, speech, streamerId, backgroundColor);

            Assert.Equal(id, reward.Id);
            Assert.Null(reward.ImageUrl);
            Assert.Equal(title, reward.Title);
            Assert.Equal(speech, reward.Speech);
            Assert.Equal(prompt, reward.Prompt);
            Assert.Equal(streamerId, reward.StreamerId);
            Assert.Equal(backgroundColor, reward.BackgroundColor);
            Assert.Empty(reward.Redemptions);
        }

        [Fact]
        public void Reward_EnableTextToSpeech()
        {
            var reward = CreateReward();

            reward.EnableTextToSpeech();

            Assert.True(reward.Speech.Enabled);
        }

        [Fact]
        public void Reward_DisableTextToSpeech()
        {
            var reward = CreateReward();

            reward.EnableTextToSpeech();
            reward.DisableTextToSpeech();

            Assert.False(reward.Speech.Enabled);
        }

        [Fact]
        public void Reward_AddAsset_Throws_DuplicateAsset()
        {
            var reward = CreateReward();
            var fileName = FileName.FromString(MP4FILE);

            reward.AddAsset(fileName, 100);

            Assert.Throws<DuplicateAssetException>(() => reward.AddAsset(fileName, 100));
        }

        [Fact]
        public void Reward_AddAsset()
        {
            var reward = CreateReward();
            var fileName = FileName.FromString(MP4FILE);

            reward.AddAsset(fileName, 100);

            Assert.NotEmpty(reward.Assets);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Reward_GetAsset_Throws_InvalidArgs(string? name)
        {
            var reward = CreateReward();

            Assert.ThrowsAny<ArgumentException>(() => reward.GetAsset(name));
        }

        [Fact]
        public void Reward_GetAsset()
        {
            var reward = CreateReward();
            var fileName = FileName.FromString(MP4FILE);
            reward.AddAsset(fileName, 100);

            var asset = reward.GetAsset(fileName.ToString());

            Assert.Equal(asset?.ToString(), fileName.ToString());
        }

        [Fact]
        public void Reward_TryGetRandomAsset_False()
        {
            var reward = CreateReward();

            var exists = reward.TryGetRandomAsset(out var asset);

            Assert.Null(asset);
            Assert.False(exists);
        }

        [Fact]
        public void Reward_TryGetRandomAsset_True()
        {
            var reward = CreateReward();
            var fileName = FileName.FromString(MP4FILE);
            reward.AddAsset(fileName, 100);

            var exists = reward.TryGetRandomAsset(out var asset);

            Assert.True(exists);
            Assert.NotNull(asset);
            Assert.Equal(asset.ToString(), fileName.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Reward_RemoveAsset_Throws_InvalidArgs(string? name)
        {
            var reward = CreateReward();

            Assert.ThrowsAny<ArgumentException>(() => reward.RemoveAsset(name));
        }

        [Fact]
        public void Reward_RemoveAsset()
        {
            var reward = CreateReward();
            var fileName = FileName.FromString(MP4FILE);
            reward.AddAsset(fileName, 100);

            reward.RemoveAsset(fileName.ToString());

            Assert.Empty(reward.Assets);
        }

        [Fact]
        public void Reward_Equal()
        {
            var id = Guid.NewGuid().ToString();
            var streamerId = Guid.NewGuid().ToString();

            var reward = CreateReward(id, streamerId);
            var reward1 = CreateReward(id, streamerId);

            Assert.Equal(reward, reward1);
        }

        [Fact]
        public void Reward_NotEqual()
        {
            var id = Guid.NewGuid().ToString();
            var streamerId = Guid.NewGuid().ToString();

            var reward = CreateReward(id, streamerId);
            var reward1 = CreateReward();

            Assert.NotEqual(reward, reward1);
        }

        private static Reward CreateReward(
            string? id = null,
            string? title = null,
            string? prompt = null,
            Speech? speech = null,
            string? streamerId = null,
            string? backgroundColor = null)
        {
            return new Reward
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Title = title ?? "Test",
                Prompt = prompt ?? "N/A",
                Speech = speech ?? new Speech(),
                StreamerId = streamerId ?? Guid.NewGuid().ToString(),
                BackgroundColor = backgroundColor ?? "blue"
            };
        }
    }
}
