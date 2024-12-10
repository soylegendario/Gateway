using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Gateway.Api;

public class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Basic"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        var encodedUsernamePassword = authHeader["Basic ".Length..].Trim();
        var usernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
        var parts = usernamePassword.Split(':');
        if (parts.Length != 2)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }
        var username = parts[0];
        var password = parts[1];
        if (username != "admin" || password != "admin")
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }
        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}