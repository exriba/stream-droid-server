using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharpTwitch.Auth;
using SharpTwitch.Auth.Models;
using SharpTwitch.Core;
using SharpTwitch.Core.Enums;
using SharpTwitch.Helix.Models;
using SharpTwitch.Helix.Models.Channel.Reward;
using StreamDroid.Application.Settings;
using StreamDroid.Application.Tests.Middleware;
using StreamDroid.Core.Entities;
using StreamDroid.Core.Interfaces;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain;
using StreamDroid.Domain.Middleware;
using StreamDroid.Domain.Services.AssetFile;
using StreamDroid.Domain.Services.Redeem;
using StreamDroid.Domain.Services.Reward;
using StreamDroid.Domain.Services.User;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure;
using StreamDroid.Shared;
using System.Text.Json;
using System.Text.Json.Nodes;
using Helix = SharpTwitch.Helix.Models;
using HelixModels = SharpTwitch.Helix.Models;

namespace StreamDroid.Application.Tests.Common
{
    public sealed class TestFixture : IAsyncLifetime
    {
        private readonly WebApplication _webApplication;

        internal readonly string userId = "1";
        internal readonly string rewardId = "4396f91d-0453-488b-873b-e016f461a297";
        internal readonly GrpcChannel grpcChannel;

        public TestFixture()
        {
            var dictionary = new Dictionary<string, string>
            {
                { "EncryptionSettings:KeyPhrase", "w9z$C&F)H@McQfTj" },

                { "SqliteSettings:ConnectionString", "Data Source=file::memory:?cache=shared" },

                { "Kestrel:Endpoints:Http:Url", "http://localhost:8070" },
                { "Kestrel:Endpoints:Grpc_H2C_Insecure:Url", "http://localhost:8071" },
                { "Kestrel:Endpoints:Grpc_H2C_Insecure:Protocols", "Http2" },

                { "Logging:LogLevel:Default", "Debug" },
                { "Logging:LogLevel:Microsoft.AspNetCore", "Warning" },

                { "CoreSettings:RedirectUri", "http://localhost:8070/redirect" },
                { "CoreSettings:ClientId", "stream-droid-client-id" },
                { "CoreSettings:Secret", "stream-droid-client-secret" },
                { "CoreSettings:Scopes:0", "CHAT_EDIT" },
                { "CoreSettings:Scopes:1", "CHAT_READ" },
                { "CoreSettings:Scopes:2", "CHANNEL_READ_REDEMPTIONS" },
                { "CoreSettings:Scopes:3", "CHANNEL_MANAGE_REDEMPTIONS" },

                { "AppSettings:ClientUri", "N/A" },
                { "AppSettings:StaticAssetPath", "media" },
                { "AppSettings:ServerUri", "http://localhost:8070" },
                { "AppSettings:ApplicationName", "StreamDroid" },

                { "JwtSettings:SigningKey", "this-is-a-super-secret-signingkey-please-dont-steal-it" },
                { "JwtSettings:Issuer", "stream-droid-server" },
                { "JwtSettings:Audience", "stream-droid-client" }
            };

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Test"
            });

            builder.Configuration.AddInMemoryCollection(dictionary!).Build();
            builder.Configuration.Configure();
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.Key));
            builder.Services.AddSingleton<IAppSettings>(options => options.GetRequiredService<IOptions<AppSettings>>().Value);
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.Key));

            builder.Services.AddInfrastructureConfiguration(builder.Configuration);
            builder.Services.AddServiceConfiguration(builder.Configuration);

            var user = new Helix.User.User
            {
                Id = userId,
                BroadcasterType = "affiliate"
            };
            var userResponse = new HelixCollectionResponse<Helix.User.User>
            {
                Data = [user]
            };
            var helixUser = new HelixModels.User.User
            {
                BroadcasterType = string.Empty
            };
            var helixUserCollectionResponse = new HelixCollectionResponse<HelixModels.User.User>
            {
                Data = [helixUser]
            };

            var customReward = new CustomReward
            {
                Id = rewardId,
                Title = "Title",
                Prompt = "Prompt",
                BroadcasterUserId = userId,
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

            var mockApiCore = new Mock<IApiCore>();
            mockApiCore.Setup(x => x.GetAsync<HelixCollectionResponse<CustomReward>>(
                It.IsAny<UrlFragment>(),
                It.IsAny<IDictionary<Header, string>>(),
                It.IsAny<IDictionary<QueryParameter, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(customRewardResponse));
            mockApiCore.Setup(x => x.GetAsync<HelixCollectionResponse<HelixModels.User.User>>(
                It.IsAny<UrlFragment>(),
                It.IsAny<IDictionary<Header, string>>(),
                It.IsAny<IEnumerable<KeyValuePair<QueryParameter, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(helixUserCollectionResponse);
            builder.Services.AddSingleton<IApiCore>(mockApiCore.Object);

            var accessTokenResponseJson = new JsonObject
            {
                { "AccessToken", "accessToken" },
                { "RefreshToken", "refreshToken" }
            };
            var accessTokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(accessTokenResponseJson.ToString());
            var validateTokenResponseJson = new JsonObject
            {
                { "UserId", userId },
                { "Login", "user" }
            };
            var validateTokenResponse = JsonSerializer.Deserialize<ValidateTokenResponse>(validateTokenResponseJson.ToString());

            var mockAuthApi = new Mock<IAuthApi>();
            mockAuthApi.Setup(x => x.GetAccessTokenFromCodeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessTokenResponse!);
            mockAuthApi.Setup(x => x.ValidateAccessTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validateTokenResponse!);
            builder.Services.AddSingleton<IAuthApi>(mockAuthApi.Object);

            var mockAssetFileService = new Mock<IAssetFileService>();
            builder.Services.AddSingleton<IAssetFileService>(mockAssetFileService.Object);

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
            builder.Services.AddMvc();
            builder.Services.AddGrpc(options =>
            {
                options.MaxReceiveMessageSize = 4 * 1024 * 1024;
                options.MaxSendMessageSize = 4 * 1024 * 1024;
                options.EnableDetailedErrors = true;

                options.Interceptors.Add<RequestInterceptor>();
            });

            _webApplication = builder.Build();
            _webApplication.UseRouting();
            _webApplication.UseAuthentication();
            _webApplication.UseMiddleware<AuthenticationMiddleware>();
            _webApplication.UseAuthorization();
            _webApplication.MapControllers();

            _webApplication.MapGrpcService<UserService>();
            _webApplication.MapGrpcService<RewardService>();
            _webApplication.MapGrpcService<RedeemService>();

            grpcChannel = GrpcChannel.ForAddress("http://localhost:8071");
        }

        public async Task InitializeAsync()
        {
            await SeedDatabaseAsync();

            await _webApplication.StartAsync();
        }

        private async Task SeedDatabaseAsync()
        {
            var serviceScopeFactory = _webApplication.Services.GetRequiredService<IServiceScopeFactory>();

            using var scope = serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUberRepository>();

            var user = new User
            {
                Id = userId,
                Name = "user",
                AccessToken = "accessToken",
                RefreshToken = "refreshToken"
            };

            await repository.AddAsync(user);

            var reward = new Reward
            {
                Id = rewardId,
                ImageUrl = "http://localhost/image.png",
                Title = "Title",
                Prompt = "Prompt",
                Speech = new Speech(),
                StreamerId = userId,
                BackgroundColor = "#6441A4",
            };
            var fileName = FileName.FromString("file.mp3");
            var fileName2 = FileName.FromString("file.mp4");
            reward.AddAsset(fileName, 50);
            reward.AddAsset(fileName2, 50);

            await repository.AddAsync(reward);

            var redemption = new Redemption
            {
                UserId = user.Id,
                UserName = user.Name,
                Reward = reward,
            };

            await repository.AddAsync(redemption);
        }

        public async Task DisposeAsync()
        {
            grpcChannel.Dispose();
            await _webApplication.StopAsync();
            await _webApplication.DisposeAsync();
        }
    }
}
