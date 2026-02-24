using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharpTwitch.Auth;
using StreamDroid.Core.Exceptions;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.Policies;
using StreamDroid.Domain.Settings;
using StreamDroid.Shared.Extensions;
using System.Security.Claims;
using System.Text;
using Entities = StreamDroid.Core.Entities;

namespace StreamDroid.Domain.Services.User
{
    public sealed class UserManager : IUserManager
    {
        private const string ID = "Id";
        private const string NAME = "Name";
        private const string JWT_ID = "jti";

        private readonly IAuthApi _authApi;
        private readonly JwtSettings _jwtSettings;
        private readonly IUberRepository _repository;

        public UserManager(IAuthApi authApi,
                           IOptions<JwtSettings> options,
                           IUberRepository repository)
        {
            _authApi = authApi;
            _repository = repository;
            _jwtSettings = options.Value;
        }

        /// <inheritdoc/>
        public async Task<Entities.User> FetchUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _repository.FindByIdAsync<Entities.User>(userId, cancellationToken) ?? throw new EntityNotFoundException(userId);
        }

        /// <inheritdoc/>
        public async Task<string> GenerateAccessTokenAsync(string userId, CancellationToken cancellationToken)
        {
            var user = await FetchUserByIdAsync(userId, cancellationToken);

            var claims = new List<Claim>
            {
                new(ID, userId),
                new(NAME, user.Name),
                new(JWT_ID, Guid.NewGuid().ToString()),
            };

            var encodedKey = Encoding.UTF8.GetBytes(_jwtSettings.SigningKey);
            var securityKey = new SymmetricSecurityKey(encodedKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JsonWebTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return token;
        }

        /// <inheritdoc/>
        public async Task<TokenRefreshPolicy> CreateTokenRefreshPolicyAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await FetchUserByIdAsync(userId, cancellationToken);

            async Task<string> refreshToken(string userId)
            {
                var refreshToken = user.RefreshToken.Base64Decrypt();
                var token = await _authApi.RefreshAccessTokenAsync(refreshToken, cancellationToken);
                user.AccessToken = token.AccessToken;
                user.RefreshToken = token.RefreshToken;
                user = await _repository.UpdateAsync(user, cancellationToken);
                return token.AccessToken;
            }

            var accessToken = user.AccessToken.Base64Decrypt();
            return new TokenRefreshPolicy(userId, accessToken, refreshToken);
        }
    }
}
