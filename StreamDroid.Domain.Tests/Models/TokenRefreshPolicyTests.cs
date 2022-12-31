using SharpTwitch.Core.Exceptions;
using StreamDroid.Domain.Models;
using StreamDroid.Domain.Tests.Common;

namespace StreamDroid.Domain.Tests.Models
{
    public class TokenRefreshPolicyTests : TestFixture
    {
        private const string NEW_ACCESS_TOKEN = "NewAccessToken";

        public TokenRefreshPolicyTests() : base() { }

        [Fact]
        public async Task TokenRefreshPolicy_Execute()
        {
            var accessToken = "accessToken";
            var user = new Core.Entities.User
            {
                Id = Guid.NewGuid().ToString(),
                Name = "user",
                AccessToken = accessToken,
                RefreshToken = "refreshToken"
            };

            static Task<string> refreshToken(Core.Entities.User user) => RefreshAccessToken(user);
            var policy = new TokenRefreshPolicy(user, refreshToken);
            var token = await policy.Policy.ExecuteAsync(async context => 
            {
                if (context[accessToken].Equals(accessToken))
                    throw new UnauthorizedRequestException("error");
                return await Task.FromResult(context[accessToken].ToString());
            }, policy.ContextData);

            Assert.Equal(NEW_ACCESS_TOKEN, token);
        }

        private static Task<string> RefreshAccessToken(Core.Entities.User user)
        {
            return Task.FromResult(NEW_ACCESS_TOKEN);
        }
    }
}