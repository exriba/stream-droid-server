using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using StreamDroid.Application.Tests.Common;

namespace StreamDroid.Application.Tests.Services.Redeem
{
    [Collection(TestCollectionFixture.Definition)]
    public class RedeemServiceTests
    {
#pragma warning disable CS0436 // Type conflicts with imported type
        private readonly GrpcRedeemService.GrpcRedeemServiceClient _grpcRedeemServiceClient;
        private readonly string _rewardId;

        public RedeemServiceTests(TestFixture testFixture)
        {
            _rewardId = testFixture.rewardId;
            _grpcRedeemServiceClient = new GrpcRedeemService.GrpcRedeemServiceClient(testFixture.grpcChannel);
        }

        [Fact]
        public async Task RedeemService_FindRewardRedeemStatisticsFromUser()
        {
            var request = new Empty();

            var response = await _grpcRedeemServiceClient.FindRewardRedeemStatisticsFromUserAsync(request);

            var rewardRedeems = response.RewardRedeems;

            Assert.NotEmpty(rewardRedeems);

            var rewardRedeem = rewardRedeems.First();

            Assert.Equal("100", rewardRedeem.Percentage);
        }

        [Fact]
        public async Task RedemptionService_FindUserRedeemStatisticsByReward_RpcException_InvalidArgument()
        {
            var request = new RewardRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            var exception = await Assert.ThrowsAnyAsync<RpcException>(
                async () => await _grpcRedeemServiceClient.FindUserRedeemStatisticsByRewardAsync(request)
            );

            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task RedemptionService_FindUserRedeemStatisticsByReward()
        {
            var request = new RewardRequest
            {
                RewardId = _rewardId
            };

            var response = await _grpcRedeemServiceClient.FindUserRedeemStatisticsByRewardAsync(request);

            var userRedeems = response.UserRedeems;

            Assert.NotEmpty(userRedeems);

            var userRedeem = userRedeems.First();

            Assert.Equal(1, userRedeem.RedeemCount);
            Assert.Equal("100", userRedeem.Percentage);
        }
#pragma warning restore CS0436 // Type conflicts with imported type
    }
}
