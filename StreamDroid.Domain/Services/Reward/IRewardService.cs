using StreamDroid.Core.ValueObjects;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.Reward
{
    public interface IRewardService
    {
        Task SyncRewards(string userId);
        Entities.Reward FindRewardById(string rewardId);
        IReadOnlyCollection<Entities.Reward> FindRewardsByUserId(string userId);
        Entities.Reward UpdateRewardSpeech(string rewardId, Speech speech);
        Tuple<string, IReadOnlyCollection<Asset>> AddRewardAssets(string rewardId, IDictionary<FileName, int> fileMap);
        void RemoveRewardAssets(string rewardId, IEnumerable<FileName> fileNames);
    }
}
