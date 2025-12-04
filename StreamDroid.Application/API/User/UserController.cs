using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharpTwitch.Auth.Helpers;
using SharpTwitch.Core.Settings;
using StreamDroid.Application.API.Constraints;
using StreamDroid.Application.Settings;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Services.User;
using StreamDroid.Shared.Extensions;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace StreamDroid.Application.API.User
{
    /// <summary>
    /// User controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("/")]
    public class UserController : Controller
    {
        private const string ID = "Id";
        private const string NAME = "Name";
        private const string REFERER = "Referer";

        private const string ISSUER = "iss";
        private const string AUDIENCE = "aud";
        private const string JWT_ID = "jti";
        private const string ACCESS_TOKEN = "accessToken";

        private readonly JwtSettings _jwtSettings;
        private readonly IUserService _userService;
        private readonly ICoreSettings _coreSettings;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService,
                              ICoreSettings coreSettings,
                              IOptions<JwtSettings> jwtSettings,
                              ILogger<UserController> logger)
        {
            _logger = logger;
            _userService = userService;
            _coreSettings = coreSettings;
            _jwtSettings = jwtSettings.Value;
        }

        /// <summary>
        /// Handles login.
        /// </summary>
        /// <returns>Redirects to authentication url.</returns>
        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login()
        {
            var referer = HttpContext.Request.Headers[REFERER];
            var state = referer.ToString();
            var encryptedState = state.Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);
            var loginUrl = AuthUtils.GenerateAuthorizationUrl(_coreSettings.ClientId, _coreSettings.RedirectUri, _coreSettings.Scopes, encodedState);
            return Ok(loginUrl);
        }

        // TODO: Mark token for invalidation
        /// <summary>
        /// Handles logout.
        /// </summary>
        //[HttpPost("logout")]
        //public async Task<IActionResult> LogoutAsync()
        //{
        //    await HttpContext.SignOutAsync();
        //    return Ok();
        //}

        /// <summary>
        /// Handles successful authentication.
        /// </summary>
        /// <param name="code">code</param>
        /// <param name="state">state</param>
        /// <returns>Redirects to referer url.</returns>
        [AllowAnonymous]
        [HttpGet("redirect")]
        [QueryParameter("code", "state")]
        public async Task<IActionResult> AuthenticationSuccessAsync([FromQuery] string code,
                                                                    [FromQuery] string state)
        {
            var encryptedState = Base64UrlEncoder.Decode(state);
            var referer = encryptedState.Base64Decrypt();

            var user = await _userService.AuthenticateUserAsync(code);

            var claims = new List<Claim>
            {
                new Claim(ID, user.Id.ToString()),
                new Claim(NAME, user.Name),
                new Claim(AUDIENCE, _jwtSettings.Audience),
                new Claim(ISSUER, _jwtSettings.Issuer),
                new Claim(JWT_ID, Guid.NewGuid().ToString()),
            };

            var encodedKey = Encoding.UTF8.GetBytes(_jwtSettings.SigningKey);
            var securityKey = new SymmetricSecurityKey(encodedKey);

            var jwtToken = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.WriteToken(jwtToken);

            var uriBuilder = new UriBuilder(referer);
            var queryParameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryParameters[ACCESS_TOKEN] = token;
            uriBuilder.Query = queryParameters.ToString();

            _logger.LogInformation("{user} logged in.", user.Name);
            return Redirect(uriBuilder.Uri.AbsoluteUri);
        }

        /// <summary>
        /// Handles unsuccessful authentication.
        /// </summary>
        /// <param name="error">error</param>
        /// <param name="errorDescription">error description</param>
        /// <param name="state">state</param>
        /// <returns>Redirects to referer url.</returns>
        [AllowAnonymous]
        [HttpGet("redirect")]
        public IActionResult AuthenticationError([FromQuery] string error,
                                                 [FromQuery(Name = "error_description")] string errorDescription,
                                                 [FromQuery] string state)
        {
            _logger.LogError("Error ocurred during login {error}. Details: {errorDescription}.", error, errorDescription);
            var encryptedState = Base64UrlEncoder.Decode(state);
            var referer = encryptedState.Base64Decrypt();
            return Redirect(referer);
        }

        /// <summary>
        /// Finds a user by the given id.
        /// </summary>
        /// <returns>A user DTO.</returns>
        [HttpGet("me")]
        public async Task<IActionResult> FindUserByIdAsync()
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));
            var user = await _userService.FindUserByIdAsync(claim.Value);
            return Ok(user);
        }

        /// <summary>
        /// Updates user preferences for the given user.
        /// </summary>
        /// <param name="preferences">preferences</param>
        /// <returns>The user's preferences.</returns>
        [HttpPost("me/preferences")]
        public async Task<IActionResult> UpdateUserPreferencesAsync([Required][FromBody] Preferences preferences)
        {
            var claim = User.Claims.First(c => c.Type.Equals(ID));
            var userPreferences = await _userService.UpdateUserPreferencesAsync(claim.Value, preferences);
            return Ok(userPreferences);
        }
    }
}