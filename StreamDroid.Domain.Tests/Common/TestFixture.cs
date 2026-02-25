using Grpc.Core;
using Grpc.Core.Testing;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StreamDroid.Domain.DTOs;
using StreamDroid.Domain.Settings;
using StreamDroid.Shared;
using System.Security.Claims;

namespace StreamDroid.Domain.Tests.Common
{
    public sealed class TestFixture
    {
        internal readonly string userId = "userId";
        internal readonly IOptions<JwtSettings> options;
        internal readonly Func<CancellationTokenSource?, ServerCallContext> createTestServerCallContext;

        public TestFixture()
        {
            var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
            var applicationAssembly = typeof(BaseProto<,>).Assembly;
            typeAdapterConfig.Scan(applicationAssembly);

            var dictionary = new Dictionary<string, string>
            {
                { "EncryptionSettings:KeyPhrase", "w9z$C&F)H@McQfTj" },
                { "JwtSettings:SigningKey", "this-is-a-super-secret-signingkey-please-dont-steal-it" },
                { "JwtSettings:Issuer", "stream-droid-server" },
                { "JwtSettings:Audience", "stream-droid-client" }
            };

            using var configurationManager = new ConfigurationManager();
            configurationManager.AddInMemoryCollection(dictionary).Build();
            configurationManager.Configure();

            var jwtSettings = new JwtSettings();
            configurationManager.GetSection(JwtSettings.Key).Bind(jwtSettings);
            options = Options.Create(jwtSettings);

            createTestServerCallContext = CreateTestServerCallContext;
        }

        private static ServerCallContext CreateTestServerCallContext(CancellationTokenSource? source = null)
        {
            var context = TestServerCallContext.Create(
                method: "TestMethod",
                host: "localhost",
                deadline: DateTime.UtcNow.AddMinutes(1),
                requestHeaders: [],
                cancellationToken: source?.Token ?? CancellationToken.None,
                peer: "127.0.0.1",
                authContext: null,
                contextPropagationToken: null,
                writeHeadersFunc: (m) => Task.CompletedTask,
                writeOptionsGetter: () => null,
                writeOptionsSetter: (o) => { }
            );

            var claimsIdentity = new ClaimsIdentity(
                [
                    new Claim("Id", "userId"),
                    new Claim("Name", "Name")
                ]
            );
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            context.UserState["__HttpContext"] = httpContext;
            return context;
        }
    }
}
