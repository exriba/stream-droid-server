using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using StreamDroid.Domain.DTOs;
using StreamDroid.Infrastructure.Persistence;
using static GrpcRedeemService;

namespace StreamDroid.Domain.Services.Redeem
{
    /// <summary>
    /// Service class responsible for handling all Redeem related logic.
    /// </summary>
    [Authorize]
    public sealed class RedeemService : GrpcRedeemServiceBase
    {
        private const string ID = "Id";
        private const string NAME = "Name";

        private readonly IRedemptionRepository _repository;
        private readonly ILogger<RedeemService> _logger;

        public RedeemService(IRedemptionRepository repository,
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
            var idClaim = userPrincipal.Claims.First(c => c.Type.Equals(ID));
            var nameClaim = userPrincipal.Claims.First(c => c.Type.Equals(NAME));

            _logger.LogInformation("Finding redeems for user: {name}.", nameClaim.Value);

            var redeems = await _repository.FindAsync(x => x.Reward.StreamerId.Equals(idClaim.Value));
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

            var redeems = await _repository.FindAsync(x => x.Reward.Id.Equals(rewardId.ToString()));

            var userRedeemResponse = new UserRedeemResponse();

            if (redeems.Count > 0)
            {
                _logger.LogInformation("Finding redeems for redeem: {redeem}.", redeems.First().Reward.Title);

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
