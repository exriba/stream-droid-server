using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using StreamDroid.Core.Entities;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.DTOs;
using static GrpcRedeemService;

namespace StreamDroid.Domain.Services.Redeem
{
    /// <summary>
    /// Redeem Service API.
    /// </summary>
    [Authorize]
    public sealed class RedeemService : GrpcRedeemServiceBase
    {
        private const string ID = "Id";

        private readonly IUberRepository _repository;
        private readonly ILogger<RedeemService> _logger;

        public RedeemService(IUberRepository repository,
                             ILogger<RedeemService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Finds reward redeem statistics for the current user id.
        /// </summary>
        /// <returns>A collection of reward redeem statistics.</returns>
        public override async Task<RewardRedeemResponse> FindRewardRedeemStatisticsFromUser(Empty request, ServerCallContext context)
        {
            var userPrincipal = context.GetHttpContext().User;
            var claim = userPrincipal.Claims.First(c => c.Type.Equals(ID));

            var redeems = await _repository.FindListAsync<Redemption>(x => x.Reward.StreamerId.Equals(claim.Value), cancellationToken: context.CancellationToken);
            var rewardRedeems = redeems.GroupBy(x => x.Reward, (x, y) =>
            {
                var value = decimal.Divide(y.Count(), redeems.Count);
                var percentage = decimal.Multiply(value, 100);
                var roundedPercentage = decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
                var rewardRedeem = RewardRedeemProto.FromEntity(x);
                rewardRedeem.Percentage = roundedPercentage.ToString();
                return rewardRedeem;
            }).ToList();

            var rewardRedeemResponse = new RewardRedeemResponse();
            rewardRedeemResponse.RewardRedeems.AddRange(rewardRedeems);
            return rewardRedeemResponse;
        }

        /// <summary>
        /// Finds user redeem statistics by a given reward id.
        /// </summary>
        /// <returns>A collection of user redeem statistics. </returns>
        /// <exception cref="ArgumentException">If the reward id is an empty GUID</exception>
        public override async Task<UserRedeemResponse> FindUserRedeemStatisticsByReward(RewardRequest request, ServerCallContext context)
        {
            var rewardIdExists = Guid.TryParse(request.RewardId, out var rewardId);

            if (!rewardIdExists || rewardId == Guid.Empty)
            {
                throw new ArgumentException($"Invalid Reward Id: {request.RewardId}.", nameof(request.RewardId));
            }

            var redeems = await _repository.FindListAsync<Redemption>(x => x.Reward.Id.Equals(rewardId.ToString()), cancellationToken: context.CancellationToken);

            var userRedeemResponse = new UserRedeemResponse();

            if (redeems.Count > 0)
            {
                _logger.LogInformation("Finding redeems for reward: {title}.", redeems.First().Reward.Title);

                var userRedeems = redeems.GroupBy(x => x.UserId, (x, y) =>
                {
                    var value = decimal.Divide(y.Count(), redeems.Count);
                    var percentage = decimal.Multiply(value, 100);
                    var roundedPercentage = decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
                    return new Grpc.Model.UserRedeem
                    {
                        UserId = x,
                        UserName = y.First().UserName,
                        RedeemCount = y.Count(),
                        Percentage = roundedPercentage.ToString()
                    };
                }).ToList();

                userRedeemResponse.UserRedeems.AddRange(userRedeems);
            }

            return userRedeemResponse;
        }
    }
}
