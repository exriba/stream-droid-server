using Polly;
using Polly.Retry;
using SharpTwitch.Core.Exceptions;
using StreamDroid.Domain.Helpers;
using StreamDroid.Shared.Extensions;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Models
{
    public class TokenRefreshPolicy
    {
        public readonly AsyncRetryPolicy Policy;

        public readonly IDictionary<string, object> ContextData;

        public TokenRefreshPolicy(Entities.User user, Func<Entities.User, Task<string>> refreshToken)
        {
            Policy = Polly.Policy
                .Handle<UnauthorizedRequestException>()
                .RetryAsync(async (exception, retryCount, context) =>
                {
                    var accessToken = await refreshToken(user);
                    context[Constants.ACCESS_TOKEN] = accessToken;
                });

            ContextData = new Dictionary<string, object>
            {
                {Constants.USER_ID, user.Id},
                {Constants.ACCESS_TOKEN, user.AccessToken.Base64Decrypt()},
            };
        }
    }
}
