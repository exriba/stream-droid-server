using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace StreamDroid.Application.Tests.Middleware
{
    public class AuthenticationMiddleware
    {
        private const string ID = "Id";
        private const string NAME = "Name";

        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var claims = new[]
            {
                new Claim(ID, "1"),
                new Claim(NAME, "user"),
            };

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }
    }
}
