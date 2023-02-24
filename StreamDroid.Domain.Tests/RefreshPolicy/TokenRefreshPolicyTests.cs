using SharpTwitch.Core.Exceptions;
using StreamDroid.Domain.RefreshPolicy;

namespace StreamDroid.Domain.Tests.RefreshPolicy
{
    public class TokenRefreshPolicyTests
    {
        private const string NEW_ACCESS_TOKEN = "NewAccessToken";

        [Fact]
        public async Task TokenRefreshPolicy_Execute()
        {
            var accessToken = "accessToken";
            var userId = Guid.NewGuid().ToString();

            async Task<string> refreshToken(string userId) => await Task.FromResult(NEW_ACCESS_TOKEN);
            var refreshPolicy = new TokenRefreshPolicy(userId, accessToken, refreshToken);
            
            var token = await refreshPolicy.Policy.ExecuteAsync(async context =>
            {
                if (refreshPolicy.AccessToken.Equals(accessToken))
                    throw new UnauthorizedRequestException("Invalid OAuth Token.");
                return await Task.FromResult(refreshPolicy.AccessToken);
            }, refreshPolicy.ContextData);

            Assert.Equal(NEW_ACCESS_TOKEN, token);
        }
    }
}