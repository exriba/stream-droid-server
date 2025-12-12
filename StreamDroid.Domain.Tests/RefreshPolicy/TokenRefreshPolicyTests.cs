using SharpTwitch.Core.Exceptions;
using StreamDroid.Domain.Policies;

namespace StreamDroid.Domain.Tests.RefreshPolicy
{
    public class TokenRefreshPolicyTests
    {
        [Fact]
        public async Task TokenRefreshPolicy_Execute()
        {
            var accessToken = "accessToken";
            var newAccessToken = "NewAccessToken";
            var userId = Guid.NewGuid().ToString();

            async Task<string> refreshToken(string userId) => await Task.FromResult(newAccessToken);
            var refreshPolicy = new TokenRefreshPolicy(userId, accessToken, refreshToken);

            var token = await refreshPolicy.Policy.ExecuteAsync(async context =>
            {
                if (refreshPolicy.AccessToken.Equals(accessToken))
                    throw new UnauthorizedRequestException("Invalid OAuth Token.");
                return await Task.FromResult(refreshPolicy.AccessToken);
            }, refreshPolicy.ContextData);

            Assert.Equal(newAccessToken, token);
        }
    }
}