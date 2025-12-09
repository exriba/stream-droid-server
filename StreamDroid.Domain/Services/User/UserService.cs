using Ardalis.GuardClauses;
using Google.Api;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharpTwitch.Auth;
using SharpTwitch.Auth.Helpers;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Settings;
using SharpTwitch.Helix;
using StreamDroid.Core.Enums;
using StreamDroid.Core.Exceptions;
using StreamDroid.Domain.RefreshPolicy;
using StreamDroid.Domain.Settings;
using StreamDroid.Infrastructure.Persistence;
using StreamDroid.Shared.Extensions;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using static GrpcUserService;
using Entities = StreamDroid.Core.Entities;
using GrpcUser = Grpc.Model.User;
using GrpcUserPreferences = Grpc.Model.Preferences;

// TODO: Handle logout? Maybe mark token for invalidation. Look into interceptors and validators
// TODO: Review error handling implementation. Exception Handler middleware is not going to work for gRPC, look into interceptors
namespace StreamDroid.Domain.Services.User
{
    /// <summary>
    /// Service class responsible for handling all User related logic.
    /// </summary>
    [Authorize]
    public sealed class UserService : GrpcUserServiceBase, IUserService
    {
        private const string ID = "Id";
        private const string NAME = "Name";
        private const string JWT_ID = "jti";
        private const string SUCCESS_FILE = "success.html";
        private const string ERROR_FILE = "error.html";

        private readonly IAuthApi _authApi;
        private readonly HelixApi _helixApi;
        private readonly JwtSettings _jwtSettings;
        private readonly ICoreSettings _coreSettings;
        private readonly IRepository<Entities.User> _repository;
        private readonly ILogger<UserService> _logger;

        // TODO: Might need cache solution to handle large data like Redis. For now this should be enough... maybe add InMemoryCache.
        private static ConcurrentDictionary<string, string?> _sessions = new();

        public UserService(HelixApi helixApi,
                           IAuthApi authApi,
                           IRepository<Entities.User> repository,
                           IOptions<JwtSettings> options,
                           ICoreSettings coreSettings,
                           ILogger<UserService> logger)
        {
            _authApi = authApi;
            _helixApi = helixApi;
            _repository = repository;

            _jwtSettings = options.Value;
            _coreSettings = coreSettings;
            _logger = logger;
        }

        /// <summary>
        /// Generates a login authorization url.
        /// </summary>
        /// <param name="request">Session request containing session id.</param>
        /// <param name="context">Context for server-side request.</param>
        /// <returns>The authorization url.</returns>
        [AllowAnonymous]
        public override Task<LoginUrlResponse> GenerateLoginUrl(SessionRequest request, ServerCallContext context)
        {
            _sessions.TryAdd(request.SessionId, null);
            var encryptedState = request.SessionId.Base64Encrypt();

            var encodedState = Base64UrlEncoder.Encode(encryptedState);
            var loginUrl = AuthUtils.GenerateAuthorizationUrl(_coreSettings.ClientId, _coreSettings.RedirectUri, _coreSettings.Scopes, encodedState);
            var loginResponse = new LoginUrlResponse
            {
                SessionId = request.SessionId,
                AuthorizationUrl = loginUrl,
            };

            return Task.FromResult(loginResponse);
        }

        /// <summary>
        /// Handles external authentication.
        /// </summary>
        /// <param name="request">Authentication request containing code, state, error and error description.</param>
        /// <param name="context">Context for server-side request.</param>
        /// <returns>An authentication message indicating success or error.</returns>
        [AllowAnonymous]
        public override async Task<HttpBody> AuthenticateUser(AuthenticationRequest request, ServerCallContext context)
        {
            var encryptedState = Base64UrlEncoder.Decode(request.State);
            var state = encryptedState.Base64Decrypt();
            var sessionExists = _sessions.TryGetValue(state, out var _);

            if (!sessionExists)
            {
                _logger.LogError("Unable to find session: {state}.", state);
                return await CreateHttpBodyResponse(ERROR_FILE);
            }

            _logger.LogInformation("Received authentication callback for session: {sessionId}.", state);

            if (request.Error != string.Empty)
            {
                _logger.LogError("Error ocurred during login {error}. Details: {errorDescription}.", request.Error, request.ErrorDescription);
                return await CreateHttpBodyResponse(ERROR_FILE);
            }

            var user = await AuthenticateUserAsync(request.Code);

            var claims = new List<Claim>
            {
                new(ID, user.Id.ToString()),
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
            _sessions[state] = token;

            _logger.LogInformation("{user} logged in.", user.Name);
            return await CreateHttpBodyResponse(SUCCESS_FILE);
        }

        /// <summary>
        /// Monitors the authentication session status.
        /// </summary>
        /// <param name="request">Session request containing session id.</param>
        /// <param name="responseStream">Authentication request updates indicating the status of the session.</param>
        /// <param name="context">Context for server-side request.</param>
        [AllowAnonymous]
        public override async Task MonitorAuthenticationSessionStatus(SessionRequest request, IServerStreamWriter<SessionStatus> responseStream, ServerCallContext context)
        {
            var sessionExists = _sessions.TryGetValue(request.SessionId, out var _);

            if (!sessionExists)
            {
                await responseStream.WriteAsync(new SessionStatus
                {
                    Status = SessionStatus.Types.Status.Error,
                    Message = $"Unable to find session: {request.SessionId}."
                });
                return;
            }

            while (!context.CancellationToken.IsCancellationRequested)
            {
                _sessions.TryGetValue(request.SessionId, out var token);

                if (token is not null)
                {
                    await responseStream.WriteAsync(new SessionStatus
                    {
                        Status = SessionStatus.Types.Status.Authorized,
                        AccessToken = token,
                        Message = "Login Successful."
                    });
                    break;
                }

                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Finds the user for the current request.
        /// </summary>
        /// <param name="request">Generic empty message.</param>
        /// <param name="context">Context for server-side request.</param>
        /// <returns>A user response.</returns>
        public override async Task<UserResponse> FindUser(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            var usePrincipal = context.GetHttpContext().User;
            var claim = usePrincipal.Claims.First(c => c.Type.Equals(ID));

            var user = await FetchUserByIdAsync(claim.Value);

            bool parsed = Enum.TryParse(user.UserType.Name, true, out GrpcUser.Types.UserType userType);

            return new UserResponse
            {
                User = new GrpcUser
                {
                    Id = user.Id,
                    Name = user.Name,
                    UserKey = user.UserKey.ToString(),
                    UserType = parsed ? userType : GrpcUser.Types.UserType.Unspecified,
                    Preferences = new GrpcUserPreferences
                    {
                        DefaultVolume = user.Preferences.DefaultVolume
                    }
                }
            };
        }

        /// <inheritdoc/>
        public async Task<TokenRefreshPolicy> CreateTokenRefreshPolicyAsync(string userId)
        {
            var user = await FetchUserByIdAsync(userId);

            async Task<string> refreshToken(string userId)
            {
                var refreshToken = user.RefreshToken.Base64Decrypt();
                var token = await _authApi.RefreshAccessTokenAsync(refreshToken, CancellationToken.None);
                user.AccessToken = token.AccessToken;
                user.RefreshToken = token.RefreshToken;
                user = await _repository.UpdateAsync(user);
                return token.AccessToken;
            }
            ;

            var accessToken = user.AccessToken.Base64Decrypt();
            return new TokenRefreshPolicy(userId, accessToken, refreshToken);
        }

        /// <summary>
        /// Authenticates the user for the given code.
        /// </summary>
        /// <param name="code">code</param>
        /// <returns>A user.</returns>
        /// <exception cref="ArgumentNullException">If the code is null</exception>
        /// <exception cref="ArgumentException">If the code is an empty or whitespace string</exception>
        private async Task<Entities.User> AuthenticateUserAsync(string code)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));

            var token = await _authApi.GetAccessTokenFromCodeAsync(code, CancellationToken.None);
            var userData = await _authApi.ValidateAccessTokenAsync(token.AccessToken, CancellationToken.None);
            var userDetailsTask = _helixApi.Users.GetUsersAsync([], token.AccessToken, CancellationToken.None);
            var userTask = FetchUserByIdAsync(userData.UserId);

            await Task.WhenAll(userDetailsTask, userTask);

            var user = userTask.Result;
            var userDetails = userDetailsTask.Result;
            var userBroadcasterType = userDetails.First().UserBroadcasterType;

            if (user is null)
            {
                user = new Entities.User
                {
                    Id = userData.UserId,
                    Name = userData.Login,
                    UserType = ConvertUserBroadcasterType(userBroadcasterType),
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken,
                };
                user = await _repository.AddAsync(user);
                return user;
            }

            user.Name = userData.Login;
            user.AccessToken = token.AccessToken;
            user.RefreshToken = token.RefreshToken;
            user.UserType = ConvertUserBroadcasterType(userBroadcasterType);
            user = await _repository.UpdateAsync(user);
            return user;
        }

        #region Helpers
        /// <summary>
        /// Finds a user by the given id.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>A user entity.</returns>
        /// <exception cref="ArgumentNullException">If the user id is null</exception>
        /// <exception cref="ArgumentException">If the user id is an empty or whitespace string</exception>
        /// <exception cref="EntityNotFoundException">If the user is not found</exception>
        private async Task<Entities.User> FetchUserByIdAsync(string userId)
        {
            Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

            return await _repository.FindByIdAsync(userId) ?? throw new EntityNotFoundException(userId);
        }

        /// <summary>
        /// Simple converter from <see cref="BroadcasterType"/> to <see cref="UserType"/>.
        /// </summary>
        /// <param name="userBroadcasterType">the user broadcaster type</param>
        /// <returns>A UserType.</returns>
        /// <exception cref="ArgumentException">If the user broadcaster type is invalid or unknown.</exception>
        private static UserType ConvertUserBroadcasterType(BroadcasterType userBroadcasterType)
        {
            return userBroadcasterType switch
            {
                BroadcasterType.NORMAL => UserType.NORMAL,
                BroadcasterType.AFFILIATE => UserType.AFFILIATE,
                BroadcasterType.PARTNER => UserType.PARTNER,
                _ => throw new ArgumentException($"Invalid User Broadcaster Type ({userBroadcasterType})")
            };
        }

        /// <summary>
        /// Simple <see cref="HttpBody"/> generator to render server side html files.
        /// </summary>
        /// <param name="fileName">the name of the file to render</param>
        /// <returns>An HttpBody</returns>
        private static async Task<HttpBody> CreateHttpBodyResponse(string fileName)
        {
            var rootPath = AppContext.BaseDirectory;
            var filePath = Path.Combine(rootPath, "Templates", fileName);
            var htmlContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var htmlBytes = Encoding.UTF8.GetBytes(htmlContent);
            var byteString = ByteString.CopyFrom(htmlBytes);

            return new HttpBody
            {
                ContentType = "text/html",
                Data = byteString
            };
        }
        #endregion
    }
}
