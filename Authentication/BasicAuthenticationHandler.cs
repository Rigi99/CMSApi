using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValue)
            || string.IsNullOrWhiteSpace(authHeaderValue))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

        AuthenticationHeaderValue? authHeader;
        try
        {
            authHeader = AuthenticationHeaderValue.Parse(authHeaderValue.ToString());
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        if (!authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Scheme"));

        var parameter = authHeader.Parameter;
        if (string.IsNullOrEmpty(parameter))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

        string username, password;
        try
        {
            var credentialBytes = Convert.FromBase64String(parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2)
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

            username = credentials[0];
            password = credentials[1];
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        var cmsUser = _configuration["BasicAuth:BasicUsername"];
        var cmsPass = _configuration["BasicAuth:BasicPassword"];
        var apiUser = _configuration["BasicAuth:AdminUsername"];
        var apiPass = _configuration["BasicAuth:AdminPassword"];

        if ((username == cmsUser && password == cmsPass) || (username == apiUser && password == apiPass))
        {
            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));
    }
}