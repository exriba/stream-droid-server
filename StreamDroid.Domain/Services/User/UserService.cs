using Ardalis.GuardClauses;
using Google.Api;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SharpTwitch.Auth;
using SharpTwitch.Auth.Helpers;
using SharpTwitch.Core.Enums;
using SharpTwitch.Core.Settings;
using SharpTwitch.Helix;
using StreamDroid.Core.Enums;
using StreamDroid.Core.Interfaces;
using StreamDroid.Domain.DTOs;
using StreamDroid.Shared.Extensions;
using System.Collections.Concurrent;
using System.Text;
using static GrpcUserService;
using Entities = StreamDroid.Core.Entities;
using GrpcUser = Grpc.Model.User;

// TODO: Handle logout? Maybe mark token for invalidation. Look into interceptors and validators
// TODO: Review error handling implementation. Exception Handler middleware is not going to work for gRPC, look into interceptors
namespace StreamDroid.Domain.Services.User
{
    /// <summary>
    /// User Service API.
    /// </summary>
    [Authorize]
    public sealed class UserService : GrpcUserServiceBase
    {
        private const string ID = "Id";
        private const string SUCCESS_FILE = "success.html";
        private const string ERROR_FILE = "error.html";

        private readonly IAuthApi _authApi;
        private readonly HelixApi _helixApi;
        private readonly IUserManager _userManager;
        private readonly ICoreSettings _coreSettings;
        private readonly ILogger<UserService> _logger;
        private readonly IRepository<Entities.User> _repository;

        // TODO: Might need cache solution to handle large data like Redis. For now this should be enough... maybe add InMemoryCache.
        private static readonly ConcurrentDictionary<string, string> _completedSessions = new();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _pendingSessions = new();

        public UserService(HelixApi helixApi,
                           IAuthApi authApi,
                           IUserManager userManager,
                           ICoreSettings coreSettings,
                           IRepository<Entities.User> repository,
                           ILogger<UserService> logger)
        {
            _authApi = authApi;
            _helixApi = helixApi;
            _userManager = userManager;
            _coreSettings = coreSettings;
            _repository = repository;
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
            var semaphore = new SemaphoreSlim(0, 1);
            _pendingSessions.TryAdd(request.SessionId, semaphore);
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

            var sessionExists = _pendingSessions.TryGetValue(state, out _);

            if (!sessionExists)
            {
                _logger.LogError("Unable to find session: {state}.", state);
                return await CreateHttpBodyResponse(ERROR_FILE, context.CancellationToken);
            }

            _logger.LogInformation("Received authentication callback for session: {sessionId}.", state);

            if (request.Error != string.Empty)
            {
                _pendingSessions.TryRemove(state, out _);
                _logger.LogError("Error ocurred during login {error}. Details: {errorDescription}.", request.Error, request.ErrorDescription);
                return await CreateHttpBodyResponse(ERROR_FILE, context.CancellationToken);
            }

            var user = await AuthenticateUserAsync(request.Code, context.CancellationToken);
            var token = await _userManager.GenerateAccessTokenAsync(user.Id, context.CancellationToken);

            _completedSessions.TryAdd(state, token);
            _pendingSessions.TryRemove(state, out var semaphore);
            semaphore?.Release();
            semaphore?.Dispose();

            _logger.LogInformation("{user} logged in.", user.Name);
            return await CreateHttpBodyResponse(SUCCESS_FILE, context.CancellationToken);
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
            var sessionExists = _pendingSessions.TryGetValue(request.SessionId, out var semaphore);

            if (!sessionExists)
            {
                await responseStream.WriteAsync(new SessionStatus
                {
                    Status = SessionStatus.Types.Status.Error,
                    Message = $"Unable to find session: {request.SessionId}."
                }, context.CancellationToken);
                return;
            }

            await semaphore!.WaitAsync(context.CancellationToken);
            _completedSessions.TryRemove(request.SessionId, out var token);
            await responseStream.WriteAsync(new SessionStatus
            {
                Status = SessionStatus.Types.Status.Authorized,
                AccessToken = token,
                Message = "Login Successful."
            }, context.CancellationToken);
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

            var user = await _userManager.FetchUserByIdAsync(claim.Value, context.CancellationToken);

            bool parsed = Enum.TryParse(user.UserType.Name, true, out GrpcUser.Types.UserType userType);

            return new UserResponse
            {
                User = UserProto.FromEntity(user)
            };
        }

        /// <summary>
        /// Authenticates the user for the given code.
        /// </summary>
        /// <param name="code">code</param>
        /// <returns>A user.</returns>
        /// <exception cref="ArgumentNullException">If the code is null</exception>
        /// <exception cref="ArgumentException">If the code is an empty or whitespace string</exception>
        private async Task<Entities.User> AuthenticateUserAsync(string code, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));

            var token = await _authApi.GetAccessTokenFromCodeAsync(code, cancellationToken);
            var userData = await _authApi.ValidateAccessTokenAsync(token.AccessToken, cancellationToken);
            var userDetailsTask = _helixApi.Users.GetUsersAsync([], token.AccessToken, cancellationToken);
            var userTask = _userManager.FetchUserByIdAsync(userData.UserId, cancellationToken);

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
                user = await _repository.AddAsync(user, cancellationToken);
                return user;
            }

            user.Name = userData.Login;
            user.AccessToken = token.AccessToken;
            user.RefreshToken = token.RefreshToken;
            user.UserType = ConvertUserBroadcasterType(userBroadcasterType);
            user = await _repository.UpdateAsync(user, cancellationToken);
            return user;
        }

        #region Helpers
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
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>An HttpBody</returns>
        private static async Task<HttpBody> CreateHttpBodyResponse(string fileName, CancellationToken cancellationToken = default)
        {
            var rootPath = AppContext.BaseDirectory;
            var filePath = Path.Combine(rootPath, "Templates", fileName);
            var htmlContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
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
