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
using StreamDroid.Core.Interfaces;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Policies;
using StreamDroid.Domain.Services.AssetFile;
using StreamDroid.Domain.Services.Reward;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Tests.Common;
using System.Linq.Expressions;
using System.Security.Claims;
using Entities = StreamDroid.Core.Entities;
using GrpcSpeech = Grpc.Model.Speech;
using Helix = SharpTwitch.Helix.Models;

namespace StreamDroid.Domain.Tests.Services.Reward
{
    [Collection(TestCollectionFixture.Definition)]
    public class RewardServiceTests
    {
        private readonly Mock<IApiCore> _mockApiCore;
        private readonly Mock<IUserManager> _mockUserManager;
        private readonly Mock<ICoreSettings> _mockCoreSettings;
        private readonly Mock<IRepository<Entities.Reward>> _mockRepository;

        private readonly RewardService _rewardService;
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

        public RewardServiceTests()
        {
            _mockApiCore = new Mock<IApiCore>();
            _mockUserManager = new Mock<IUserManager>();
            _mockCoreSettings = new Mock<ICoreSettings>();
            _mockRepository = new Mock<IRepository<Entities.Reward>>();

            var mockLogger = new Mock<ILogger<RewardService>>();
            var mockAssetFileService = new Mock<IAssetFileService>();

            var helixApi = new HelixApi(_mockCoreSettings.Object, _mockApiCore.Object);
            _rewardService = new RewardService(helixApi, _mockUserManager.Object, _mockRepository.Object, mockAssetFileService.Object, mockLogger.Object);
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
            var rewardId = Guid.NewGuid();
            var rewards = SetupRewards(rewardId);
            var reward = rewards.FirstOrDefault();

            var request = new RewardRequest
            {
                RewardId = rewardId.ToString()
            };

            _mockRepository.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(reward));

            var response = await _rewardService.FindReward(request, _context);

            Assert.Equal(rewardId.ToString(), response.Reward.Id);
        }

        [Fact]
        public async Task RewardService_FindUserRewards()
        {
            var id = Guid.NewGuid();
            var rewards = SetupRewards(id);
            ConfigureServerCallContext(id);

            var request = new Empty();
            var messages = new List<RewardResponse>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

            _mockRepository.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Entities.Reward, bool>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards));

            await _rewardService.FindUserRewards(request, mockStreamWriter.Object, _context);

            Assert.Single(messages);
        }

        [Fact]
        public async Task RewardService_FindUserRewards_SynchronizeAsync()
        {
            var id = Guid.NewGuid();
            var rewards = SetupRewards(id);
            ConfigureServerCallContext(id);

            var request = new Empty();
            var messages = new List<RewardResponse>();
            var mockStreamWriter = CreateServerStreamWriterMock(messages);

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

            _mockUserManager.Setup(x => x.CreateTokenRefreshPolicyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenRefreshPolicy);
            _mockApiCore.Setup(x => x.GetAsync<HelixCollectionResponse<Helix.User.User>>(
                It.IsAny<UrlFragment>(),
                It.IsAny<IDictionary<Header, string>>(),
                It.IsAny<IEnumerable<KeyValuePair<QueryParameter, string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(userResponse));
            _mockApiCore.Setup(x => x.GetAsync<HelixCollectionResponse<CustomReward>>(
                It.IsAny<UrlFragment>(),
                It.IsAny<IDictionary<Header, string>>(),
                It.IsAny<IDictionary<QueryParameter, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(customRewardResponse));
            _mockRepository.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Entities.Reward, bool>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards));
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<Entities.Reward>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.First()));

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
            var rewards = SetupRewards(id);
            ConfigureServerCallContext(id);

            var requests = new List<AddRewardAssetRequest>
            {
                new AddRewardAssetRequest
                {
                    RewardId = id.ToString(),
                    File = ByteString.Empty,
                    FileName = "file.mp4",
                    Volume = 50
                }
            };
            var mockStreamReader = CreateStreamReaderMock(requests);

            _mockRepository.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.FirstOrDefault()));
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Entities.Reward>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.First()));

            var response = await _rewardService.AddRewardAssets(mockStreamReader.Object, _context);

            Assert.Equal(id.ToString(), response.Reward.Id);
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
            var rewards = SetupRewards(id);
            var request = new RewardSpeechRequest
            {
                RewardId = id.ToString(),
                Speech = new GrpcSpeech
                {
                    Enabled = true,
                    VoiceIndex = 0
                }
            };

            _mockRepository.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.FirstOrDefault()));
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Entities.Reward>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.First()));

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
            var rewards = SetupRewards(id);
            ConfigureServerCallContext(id);

            var fileName = FileName.FromString("file.mp3");
            var request = new UpdateRewardAssetRequest
            {
                RewardId = id.ToString(),
                FileName = fileName.ToString(),
                Volume = 50
            };

            _mockRepository.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.FirstOrDefault()));
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Entities.Reward>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.First()));

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
            var rewards = SetupRewards(id);
            ConfigureServerCallContext(id);

            var request = new RemoveRewardAssetRequest
            {
                RewardId = id.ToString()
            };
            request.FileName.Add("file.mp3");

            _mockRepository.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.FirstOrDefault()));
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Entities.Reward>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(rewards.First()));

            var response = await _rewardService.RemoveRewardAssets(request, _context);

            Assert.Empty(response.Reward.Assets);
        }

        #region Helpers
        private static IReadOnlyCollection<Entities.Reward> SetupRewards(Guid id)
        {
            var reward = new Entities.Reward
            {
                Id = id.ToString(),
                ImageUrl = "http://localhost/image.png",
                Title = "Title",
                Prompt = "Prompt",
                Speech = new Speech(),
                StreamerId = id.ToString(),
                BackgroundColor = "#6441A4",
            };
            reward.AddAsset(FileName.FromString("file.mp3"), 100);
            return [reward];
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
            mockStreamWriter.Setup(x => x.WriteAsync(It.IsAny<RewardResponse>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback((RewardResponse rewardResponse, CancellationToken token) => messages.Add(rewardResponse));
            return mockStreamWriter;
        }

        private static Mock<IAsyncStreamReader<AddRewardAssetRequest>> CreateStreamReaderMock(IEnumerable<AddRewardAssetRequest> requests)
        {
            var enumerator = requests.GetEnumerator();

            var mockStreamReader = new Mock<IAsyncStreamReader<AddRewardAssetRequest>>();
            mockStreamReader.Setup(x => x.MoveNext(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(() => enumerator.MoveNext());
            mockStreamReader.Setup(x => x.Current)
                            .Returns(() => enumerator.Current);

            return mockStreamReader;
        }
        #endregion
    }
}
