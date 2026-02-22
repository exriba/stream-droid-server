using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Model;
using StreamDroid.Application.Tests.Common;

namespace StreamDroid.Application.Tests.Services.Reward
{
    [Collection(TestCollectionFixture.Definition)]
    public class RewardServiceTests
    {
#pragma warning disable CS0436 // Type conflicts with imported type
        private readonly GrpcRewardService.GrpcRewardServiceClient _grpcRewardServiceClient;
        private readonly string _rewardId;

        public RewardServiceTests(TestFixture testFixture)
        {
            _rewardId = testFixture.rewardId;
            _grpcRewardServiceClient = new GrpcRewardService.GrpcRewardServiceClient(testFixture.grpcChannel);
        }

        [Fact]
        public async Task RewardService_FindReward_RpcException_InvalidArgument()
        {
            var request = new RewardRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            var exception = await Assert.ThrowsAnyAsync<RpcException>(
                async () => await _grpcRewardServiceClient.FindRewardAsync(request)
            );

            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task RewardService_FindReward()
        {
            var request = new RewardRequest
            {
                RewardId = _rewardId
            };

            var response = await _grpcRewardServiceClient.FindRewardAsync(request);

            Assert.Equal(_rewardId, response.Reward.Id);
            Assert.NotEmpty(response.Reward.Assets);
        }

        [Fact]
        public async Task RewardService_FindUserRewards()
        {
            var request = new Empty();

            await foreach (var response in _grpcRewardServiceClient.FindUserRewards(request).ResponseStream.ReadAllAsync())
            {
                Assert.Equal(_rewardId, response.Reward.Id);
            }
        }

        [Fact]
        public async Task RewardService_UpdateRewardSpeech_RpcException_InvalidArgument()
        {
            var request = new RewardSpeechRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            var exception = await Assert.ThrowsAnyAsync<RpcException>(
                async () => await _grpcRewardServiceClient.UpdateRewardSpeechAsync(request)
            );

            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task RewardService_UpdateRewardSpeech()
        {
            var request = new RewardSpeechRequest
            {
                RewardId = _rewardId,
                Speech = new Speech
                {
                    Enabled = true,
                    VoiceIndex = 0,
                }
            };

            var response = await _grpcRewardServiceClient.UpdateRewardSpeechAsync(request);

            Assert.Equal(_rewardId, response.Reward.Id);
            Assert.True(response.Reward.Speech.Enabled);
        }

        [Fact]
        public async Task RewardService_AddRewardAssets_RpcException_InvalidArgument()
        {
            var data = Array.Empty<byte>();
            var byteString = ByteString.CopyFrom(data);

            var request = new AddRewardAssetRequest
            {
                RewardId = Guid.Empty.ToString(),
                FileName = "files.mp3",
                Volume = 50,
                File = byteString
            };

            var exception = await Assert.ThrowsAnyAsync<RpcException>(async () =>
            {
                var call = _grpcRewardServiceClient.AddRewardAssets();
                await call.RequestStream.WriteAsync(request);
                await call.RequestStream.CompleteAsync();
                var response = await call.ResponseAsync;
            });

            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task RewardService_AddRewardAssets()
        {
            var data = Array.Empty<byte>();
            var byteString = ByteString.CopyFrom(data);

            var request = new AddRewardAssetRequest
            {
                RewardId = _rewardId,
                FileName = "files.mp4",
                Volume = 50,
                File = byteString
            };

            var call = _grpcRewardServiceClient.AddRewardAssets();
            await call.RequestStream.WriteAsync(request);
            await call.RequestStream.CompleteAsync();
            var response = await call.ResponseAsync;

            Assert.Equal(_rewardId, response.Reward.Id);
            Assert.NotEmpty(response.Reward.Assets);
        }

        [Fact]
        public async Task RewardService_UpdateRewardAssets_RpcException_InvalidArgument()
        {
            var request = new UpdateRewardAssetRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            var exception = await Assert.ThrowsAnyAsync<RpcException>(
                async () => await _grpcRewardServiceClient.UpdateRewardAssetsAsync(request)
            );

            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task RewardService_UpdateRewardAssets()
        {
            var request = new UpdateRewardAssetRequest
            {
                RewardId = _rewardId,
                FileName = "file.mp3",
                Volume = 100
            };

            var response = await _grpcRewardServiceClient.UpdateRewardAssetsAsync(request);

            Assert.Equal(_rewardId, response.Reward.Id);
            Assert.NotEmpty(response.Reward.Assets);
        }

        [Fact]
        public async Task RewardService_RemoveRewardAssets_RpcException_InvalidArgument()
        {
            var request = new RemoveRewardAssetRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            var exception = await Assert.ThrowsAnyAsync<RpcException>(
                async () => await _grpcRewardServiceClient.RemoveRewardAssetsAsync(request)
            );

            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task RewardService_RemoveRewardAssets()
        {
            var request = new RemoveRewardAssetRequest
            {
                RewardId = _rewardId
            };
            request.FileName.Add("file.mp4");

            var response = await _grpcRewardServiceClient.RemoveRewardAssetsAsync(request);

            Assert.Equal(_rewardId, response.Reward.Id);
            Assert.NotEmpty(response.Reward.Assets);
        }
#pragma warning restore CS0436 // Type conflicts with imported type
    }
}
