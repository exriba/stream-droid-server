using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamDroid.Application.Helpers;
using System.Security.Claims;
using StreamDroid.Application.API.Constraints;
using StreamDroid.Application.Services;
using Microsoft.IdentityModel.Tokens;
using SharpTwitch.Auth.Helpers;
using SharpTwitch.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using StreamDroid.Core.ValueObjects;
using StreamDroid.Domain.Services.User;
using StreamDroid.Shared.Extensions;

namespace StreamDroid.Application.API.User
{
    [Authorize]
    [ApiController]
    [Route("/")]
    public class UserController : Controller
    {
        private const string REFERER = "Referer";
        private readonly IUserService _userService;
        private readonly ICoreSettings _coreSettings;
        private readonly TwitchPubSubClient _twitchPubSubClient;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ICoreSettings coreSettings, TwitchPubSubClient twitchPubSubClient, ILogger<UserController> logger)
        {
            _logger = logger;
            _userService = userService;
            _coreSettings = coreSettings;
            _twitchPubSubClient = twitchPubSubClient;
        }

        [HttpGet("me")]
        public IActionResult Index()
        {
            var claim = User.Claims.First(c => c.Type.Equals(Constants.ID));
            var me = _userService.FindById(claim.Value);
            return Ok(me);
        }

        [HttpPost("me/preferences")]
        public IActionResult UpdatePreferences([Required][FromBody] Preferences preferences)
        {
            var claim = User.Claims.First(c => c.Type.Equals(Constants.ID));
            var data = _userService.UpdatePreferences(claim.Value, preferences);
            return Ok(data);
        }

        [AllowAnonymous]
        [HttpGet("login")]  
        public IActionResult Login()
        {
            var referer = HttpContext.Request.Headers[REFERER];
            var state = referer.ToString();
            var encryptedState = state.Base64Encrypt();
            var encodedState = Base64UrlEncoder.Encode(encryptedState);
            var loginUrl = AuthUtils.GenerateAuthorizationUrl(_coreSettings.ClientId, _coreSettings.RedirectUri, _coreSettings.Scopes, encodedState);
            return Redirect(loginUrl);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("redirect")]
        [QueryParameter("code", "state")]
        public async Task<IActionResult> AuthSuccess([FromQuery] string code, 
                                                     [FromQuery] string state)
        {
            var encryptedState = Base64UrlEncoder.Decode(state);
            var referer = encryptedState.Base64Decrypt();

            var user = await _userService.Authenticate(code);

            var claims = new List<Claim>
            {
                new Claim(Constants.ID, user.Id),
                new Claim(Constants.NAME, user.Name),
            };

            var identity = new ClaimsIdentity(claims, Constants.USER);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, properties);
            _twitchPubSubClient.Connect();
            _logger.LogInformation("{user} logged in.", user.Name);
            return Redirect(referer);
        }

        [AllowAnonymous]
        [HttpGet("redirect")]
        public IActionResult AuthError([FromQuery] string error, 
                                       [FromQuery(Name = "error_description")] string errorDescription, 
                                       [FromQuery] string state)
        {
            _logger.LogError("Error ocurred during login {error}. Details: {errorDescription}.", error, errorDescription);
            var encryptedState = Base64UrlEncoder.Decode(state);
            var referer = encryptedState.Base64Decrypt();
            return Redirect(referer);
        }
    }
}