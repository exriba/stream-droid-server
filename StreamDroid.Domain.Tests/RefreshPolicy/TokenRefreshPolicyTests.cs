using SharpTwitch.Core.Exceptions;
using StreamDroid.Domain.RefreshPolicy;
using StreamDroid.Domain.Tests.Common;

namespace StreamDroid.Domain.Tests.RefreshPolicy
{
    public class TokenRefreshPolicyTests : TestFixture
    {
        private const string NEW_ACCESS_TOKEN = "NewAccessToken";

        public TokenRefreshPolicyTests() : base() { }

        [Fact]
        public async Task TokenRefreshPolicy_Execute()
        {
            var accessToken = "accessToken";
            var userId = Guid.NewGuid().ToString();

            async Task<string> refreshToken(string userId) => await Task.FromResult(NEW_ACCESS_TOKEN);
            var refreshPolicy = new TokenRefreshPolicy(userId, accessToken, refreshToken);
            
            var token = await refreshPolicy.Policy.ExecuteAsync(async context =>
            {
                if (context[accessToken].Equals(accessToken))
                    throw new UnauthorizedRequestException("error");
                return await Task.FromResult(context[accessToken].ToString());
            }, refreshPolicy.ContextData);

            Assert.Equal(NEW_ACCESS_TOKEN, token);
        }
    }
}