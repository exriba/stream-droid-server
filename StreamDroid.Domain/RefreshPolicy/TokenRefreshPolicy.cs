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

#pragma warning disable CS8603 // Possible null reference return.
        public string UserId => ContextData.Any() ? ContextData[USER_ID].ToString() : string.Empty;
        public string AccessToken => ContextData.Any() ? ContextData[ACCESS_TOKEN].ToString() : string.Empty;
#pragma warning restore CS8603 // Possible null reference return.

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
