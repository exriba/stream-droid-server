using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SharpTwitch.Core;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Settings;
using SharpTwitch.Helix;
using SharpTwitch.Helix.Models;
using SharpTwitch.Helix.Models.Channel.Reward;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.RefreshPolicy;
using StreamDroid.Domain.Services.AssetFile;
using StreamDroid.Domain.Services.Reward;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Tests.Common;
using StreamDroid.Infrastructure.Persistence;
using System.Security.Claims;
using Entities = StreamDroid.Core.Entities;
using GrpcSpeech = Grpc.Model.Speech;
using Helix = SharpTwitch.Helix.Models;

namespace StreamDroid.Domain.Tests.Services.Reward
{
    [Collection(TestCollectionFixture.Definition)]
    public class RewardServiceTests
    {
        private readonly Mock<IApiCore> _apiCore;
        private readonly RewardService _rewardService;
        private readonly Mock<IUserService> _userService;
        private readonly IRepository<Entities.Reward> _rewardRepository;
        private readonly ServerCallContext _context = TestServerCallContext.Create(
            method: "TestMethod",
            host: "localhost",
            deadline: DateTime.UtcNow.AddMinutes(1),
            requestHeaders: [],
            cancellationToken: CancellationToken.None,
            peer: "127.0.0.1",
            authContext: null,
            contextPropagationToken: null,
            writeHeadersFunc: (m) => Task.CompletedTask,
            writeOptionsGetter: () => null,
            writeOptionsSetter: (o) => { }
        );

        public RewardServiceTests(TestFixture testFixture)
        {
            _apiCore = new Mock<IApiCore>();
            _userService = new Mock<IUserService>();
            var coreSettings = new Mock<ICoreSettings>();
            _rewardRepository = testFixture.rewardRepository;
            var helixApi = new HelixApi(coreSettings.Object, _apiCore.Object);
            var mockLogger = new Mock<ILogger<RewardService>>();
            var mockAssetFileService = new Mock<IAssetFileService>();
            _rewardService = new RewardService(helixApi, _userService.Object, _rewardRepository, mockAssetFileService.Object, mockLogger.Object);
        }

        [Fact]
        public async Task RewardService_FindReward_Throws_InvalidArgs()
        {
            var request = new RewardRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _rewardService.FindReward(request, _context));
        }

        [Fact]
        public async Task RewardService_FindReward()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            var rewardId = id.ToString();
            var request = new RewardRequest
            {
                RewardId = rewardId
            };

            var response = await _rewardService.FindReward(request, _context);

            Assert.Equal(rewardId, response.Reward.Id);
        }

        [Fact]
        public async Task RewardService_FindUserRewards()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            ConfigureServerCallContext(id);

            var request = new Empty();
            var messages = new List<RewardResponse>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            await _rewardService.FindUserRewards(request, mockStreamWriter.Object, _context);

            Assert.Single(messages);
        }

        [Fact]
        public async Task RewardService_FindUserRewards_SynchronizeAsync()
        {
            var id = Guid.NewGuid();
            ConfigureServerCallContext(id);

            var user = new Helix.User.User
            {
                Id = id.ToString(),
                BroadcasterType = "affiliate"
            };

            var userResponse = new HelixCollectionResponse<Helix.User.User>
            {
                Data = [user]
            };

            var customReward = new CustomReward
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Title",
                Prompt = "Prompt",
                BroadcasterUserId = user.Id,
                BackgroundColor = "#FFFFFF",
                IsUserInputRequired = true,
                Image = new Helix.Shared.Image
                {
                    Url1x = "http://localhost/image.png"
                },
            };

            var customRewardResponse = new HelixCollectionResponse<CustomReward>
            {
                Data = [customReward]
            };

            static async Task<string> refreshToken(string userId) => await Task.FromResult("NewAccessToken");
            var tokenRefreshPolicy = new TokenRefreshPolicy(user.Id, "accessToken", refreshToken);

            _userService.Setup(x => x.CreateTokenRefreshPolicyAsync(It.IsAny<string>()))
                        .ReturnsAsync(tokenRefreshPolicy);
            _apiCore.Setup(x => x.GetAsync<HelixCollectionResponse<Helix.User.User>>(
                        It.IsAny<UrlFragment>(),
                        It.IsAny<IDictionary<Header, string>>(),
                        It.IsAny<IEnumerable<KeyValuePair<QueryParameter, string>>>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(userResponse));
            _apiCore.Setup(x => x.GetAsync<HelixCollectionResponse<CustomReward>>(
                        It.IsAny<UrlFragment>(),
                        It.IsAny<IDictionary<Header, string>>(),
                        It.IsAny<IDictionary<QueryParameter, string>>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(customRewardResponse));

            var request = new Empty();
            var messages = new List<RewardResponse>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            await _rewardService.FindUserRewards(request, mockStreamWriter.Object, _context);

            Assert.Single(messages);
        }

        [Fact]
        public async Task RewardService_AddRewardAssets_Throws_InvalidArgs()
        {
            var id = Guid.NewGuid();
            ConfigureServerCallContext(id);

            var requests = new List<AddRewardAssetRequest>
            {
                new AddRewardAssetRequest
                {
                    RewardId = Guid.Empty.ToString()
                }
            };

            var mockStreamReader = CreateStreamReaderMock(requests);

            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _rewardService.AddRewardAssets(mockStreamReader.Object, _context));
        }

        [Fact]
        public async Task RewardService_AddRewardAssets()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            var rewardId = id.ToString();
            ConfigureServerCallContext(id);

            var requests = new List<AddRewardAssetRequest>
            {
                new AddRewardAssetRequest
                {
                    RewardId = rewardId,
                    File = ByteString.Empty,
                    FileName = "file.mp4",
                    Volume = 50
                }
            };

            var mockStreamReader = CreateStreamReaderMock(requests);

            var response = await _rewardService.AddRewardAssets(mockStreamReader.Object, _context);

            Assert.Equal(rewardId, response.Reward.Id);
        }

        [Fact]
        public async Task RewardService_UpdateRewardSpeech_Throws_InvalidArgs()
        {
            var request = new RewardSpeechRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _rewardService.UpdateRewardSpeech(request, _context));
        }

        [Fact]
        public async Task RewardService_UpdateRewardSpeech()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            var rewardId = id.ToString();
            var request = new RewardSpeechRequest
            {
                RewardId = rewardId,
                Speech = new GrpcSpeech
                {
                    Enabled = true,
                    VoiceIndex = 0
                }
            };

            var response = await _rewardService.UpdateRewardSpeech(request, _context);

            Assert.Equal(request.RewardId, response.Reward.Id);
        }

        [Fact]
        public async Task RewardService_UpdateRewardAssets_Throws_InvalidArgs()
        {
            var request = new UpdateRewardAssetRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _rewardService.UpdateRewardAssets(request, _context));
        }

        [Fact]
        public async Task RewardService_UpdateRewardAssets()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            ConfigureServerCallContext(id);
            var rewardId = id.ToString();
            var request = new UpdateRewardAssetRequest
            {
                RewardId = rewardId,
                FileName = "file.mp3",
                Volume = 50
            };

            var response = await _rewardService.UpdateRewardAssets(request, _context);

            Assert.Single(response.Reward.Assets);
            Assert.Equal(50, response.Reward.Assets.Single().Volume);
        }

        [Fact]
        public async Task RewardService_RemoveRewardAssets_Throws_InvalidArgs()
        {
            var id = Guid.NewGuid();
            ConfigureServerCallContext(id);
            var request = new RemoveRewardAssetRequest
            {
                RewardId = Guid.Empty.ToString()
            };

            await Assert.ThrowsAnyAsync<ArgumentException>(async () => await _rewardService.RemoveRewardAssets(request, _context));
        }

        [Fact]
        public async Task RewardService_RemoveRewardAssets()
        {
            var id = Guid.NewGuid();
            await SetupDataAsync(id);
            ConfigureServerCallContext(id);
            var rewardId = id.ToString();
            var request = new RemoveRewardAssetRequest
            {
                RewardId = rewardId
            };
            request.FileName.Add("file.mp3");

            var response = await _rewardService.RemoveRewardAssets(request, _context);

            Assert.Empty(response.Reward.Assets);
        }

        private async Task SetupDataAsync(Guid id)
        {
            var reward = new Entities.Reward
            {
                Id = id.ToString(),
                ImageUrl = null,
                Title = "Title",
                Prompt = "Prompt",
                Speech = new Speech(),
                StreamerId = id.ToString(),
                BackgroundColor = "#6441A4",
            };
            reward.AddAsset(FileName.FromString("file.mp3"), 100);
            await _rewardRepository.AddAsync(reward);
        }

        private void ConfigureServerCallContext(Guid id)
        {
            var httpContext = new DefaultHttpContext();
            var claimsIdentity = new ClaimsIdentity();
            var idClaim = new Claim("Id", id.ToString());
            var nameClaim = new Claim("Name", "Name");
            claimsIdentity.AddClaims([idClaim, nameClaim]);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            httpContext.User = claimsPrincipal;
            _context.UserState["__HttpContext"] = httpContext;
        }

        private static Mock<IServerStreamWriter<RewardResponse>> CreateServerStreamWriterMock(List<RewardResponse> messages)
        {
            var mockStreamWriter = new Mock<IServerStreamWriter<RewardResponse>>();
            mockStreamWriter.Setup(x => x.WriteAsync(It.IsAny<RewardResponse>()))
                .Returns(Task.CompletedTask)
                .Callback<RewardResponse>(x => messages.Add(x));
            return mockStreamWriter;
        }

        private static Mock<IAsyncStreamReader<AddRewardAssetRequest>> CreateStreamReaderMock(List<AddRewardAssetRequest> requests)
        {
            var enumerator = requests.GetEnumerator();

            var mockStreamReader = new Mock<IAsyncStreamReader<AddRewardAssetRequest>>();
            mockStreamReader.Setup(x => x.MoveNext(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(() => enumerator.MoveNext());
            mockStreamReader.Setup(x => x.Current)
                            .Returns(() => enumerator.Current);

            return mockStreamReader;
        }
    }
}
