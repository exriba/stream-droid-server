using Polly;
using Polly.Retry;
using SharpTwitch.Core.Exceptions;

namespace StreamDroid.Domain.RefreshPolicy
{
    public class TokenRefreshPolicy
    {
        private const string USER_ID = "userId";
        private const string ACCESS_TOKEN = "accessToken";

        public readonly AsyncRetryPolicy Policy;
        public readonly IDictionary<string, object> ContextData = new Dictionary<string, object>();

        public string UserId => ContextData.TryGetValue(USER_ID, out var id) ? $"{id}" : string.Empty;
        public string AccessToken => ContextData.TryGetValue(ACCESS_TOKEN, out var accessToken) ? $"{accessToken}" : string.Empty;

        public TokenRefreshPolicy(string userId, string accessToken, Func<string, Task<string>> refreshToken)
        {
            Policy = Polly.Policy
                .Handle<UnauthorizedRequestException>()
                .RetryAsync(async (exception, retryCount, context) =>
                {
                    var accessToken = await refreshToken(userId);
                    ContextData[ACCESS_TOKEN] = accessToken;
                });

            ContextData.Add(USER_ID, userId);
            ContextData.Add(ACCESS_TOKEN, accessToken);
        }
    }
}
