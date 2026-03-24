using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace CMSApi.Authentication;

public class BasicAuthenticationService : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly BasicAuthOptions _options;

    public BasicAuthenticationService(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<BasicAuthOptions> authOptions)
        : base(options, logger, encoder)
    {
        _options = authOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader)
            || string.IsNullOrWhiteSpace(authHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        if (!AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        if (!headerValue.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Scheme"));
        }

        if (string.IsNullOrEmpty(headerValue.Parameter))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        var credentials = DecodeCredentials(headerValue.Parameter);
        if (credentials == null)
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

        if (!TryResolveRole(credentials.Value.username, credentials.Value.password, out var role))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, credentials.Value.username),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private (string username, string password)? DecodeCredentials(string base64Credentials)
    {
        try
        {
            var credentialBytes = Convert.FromBase64String(base64Credentials);
            var decoded = Encoding.UTF8.GetString(credentialBytes);
            var parts = decoded.Split(':', 2);
            if (parts.Length != 2) return null;
            return (parts[0], parts[1]);
        }
        catch
        {
            return null;
        }
    }

    private bool TryResolveRole(string username, string password, out string role)
    {
        if (username == _options.BasicUsername && password == _options.BasicPassword)
        {
            role = "CmsIngest";
            return true;
        }

        if (username == _options.ApiUsername && password == _options.ApiPassword)
        {
            role = "ApiUser";
            return true;
        }

        if (username == _options.AdminUsername && password == _options.AdminPassword)
        {
            role = "Admin";
            return true;
        }

        role = string.Empty;
        return false;
    }
}