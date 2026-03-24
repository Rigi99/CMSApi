using CMSApi.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Xunit;

namespace CMSApi.Tests;

public class BasicAuthenticationServiceTests
{
    private static BasicAuthenticationService CreateHandler(BasicAuthOptions authOptions)
    {
        var optionsMonitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());

        return new BasicAuthenticationService(
            optionsMonitor,
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            Options.Create(authOptions));
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(BasicAuthenticationService handler, string? authorizationHeader)
    {
        var context = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
            context.Request.Headers.Authorization = authorizationHeader;

        var scheme = new AuthenticationScheme("BasicAuthentication", "BasicAuthentication", typeof(BasicAuthenticationService));

        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    private static string BuildBasicHeader(string username, string password)
    {
        var raw = $"{username}:{password}";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return $"Basic {base64}";
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenHeaderMissing()
    {
        var handler = CreateHandler(new BasicAuthOptions
        {
            BasicUsername = "cms_ingest_user",
            BasicPassword = "11111111-1111-1111-1111-111111111111",
            ApiUsername = "consumer_user",
            ApiPassword = "33333333-3333-3333-3333-333333333333",
            AdminUsername = "admin_user",
            AdminPassword = "22222222-2222-2222-2222-222222222222"
        });

        var result = await AuthenticateAsync(handler, null);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenSchemeNotBasic()
    {
        var handler = CreateHandler(new BasicAuthOptions
        {
            BasicUsername = "cms_ingest_user",
            BasicPassword = "11111111-1111-1111-1111-111111111111",
            ApiUsername = "consumer_user",
            ApiPassword = "33333333-3333-3333-3333-333333333333",
            AdminUsername = "admin_user",
            AdminPassword = "22222222-2222-2222-2222-222222222222"
        });

        var result = await AuthenticateAsync(handler, "Bearer token");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenCredentialsMalformed()
    {
        var handler = CreateHandler(new BasicAuthOptions
        {
            BasicUsername = "cms_ingest_user",
            BasicPassword = "11111111-1111-1111-1111-111111111111",
            ApiUsername = "consumer_user",
            ApiPassword = "33333333-3333-3333-3333-333333333333",
            AdminUsername = "admin_user",
            AdminPassword = "22222222-2222-2222-2222-222222222222"
        });

        var result = await AuthenticateAsync(handler, "Basic ###not-base64###");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenCredentialsInvalid()
    {
        var handler = CreateHandler(new BasicAuthOptions
        {
            BasicUsername = "cms_ingest_user",
            BasicPassword = "11111111-1111-1111-1111-111111111111",
            ApiUsername = "consumer_user",
            ApiPassword = "33333333-3333-3333-3333-333333333333",
            AdminUsername = "admin_user",
            AdminPassword = "22222222-2222-2222-2222-222222222222"
        });

        var header = BuildBasicHeader("wrong_user", "wrong_pass");
        var result = await AuthenticateAsync(handler, header);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldSucceed_ForConfiguredBasicUser()
    {
        var handler = CreateHandler(new BasicAuthOptions
        {
            BasicUsername = "cms_ingest_user",
            BasicPassword = "11111111-1111-1111-1111-111111111111",
            ApiUsername = "consumer_user",
            ApiPassword = "33333333-3333-3333-3333-333333333333",
            AdminUsername = "admin_user",
            AdminPassword = "22222222-2222-2222-2222-222222222222"
        });

        var header = BuildBasicHeader("cms_ingest_user", "11111111-1111-1111-1111-111111111111");
        var result = await AuthenticateAsync(handler, header);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("cms_ingest_user", result.Principal!.Identity!.Name);
        Assert.Equal("CmsIngest", result.Principal.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldSucceed_ForConfiguredApiUser()
    {
        var handler = CreateHandler(new BasicAuthOptions
        {
            BasicUsername = "cms_ingest_user",
            BasicPassword = "11111111-1111-1111-1111-111111111111",
            ApiUsername = "consumer_user",
            ApiPassword = "33333333-3333-3333-3333-333333333333",
            AdminUsername = "admin_user",
            AdminPassword = "22222222-2222-2222-2222-222222222222"
        });

        var header = BuildBasicHeader("consumer_user", "33333333-3333-3333-3333-333333333333");
        var result = await AuthenticateAsync(handler, header);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("consumer_user", result.Principal!.Identity!.Name);
        Assert.Equal("ApiUser", result.Principal.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldSucceed_ForConfiguredAdminUser()
    {
        var handler = CreateHandler(new BasicAuthOptions
        {
            BasicUsername = "cms_ingest_user",
            BasicPassword = "11111111-1111-1111-1111-111111111111",
            ApiUsername = "consumer_user",
            ApiPassword = "33333333-3333-3333-3333-333333333333",
            AdminUsername = "admin_user",
            AdminPassword = "22222222-2222-2222-2222-222222222222"
        });

        var header = BuildBasicHeader("admin_user", "22222222-2222-2222-2222-222222222222");
        var result = await AuthenticateAsync(handler, header);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("admin_user", result.Principal!.Identity!.Name);
        Assert.Equal("Admin", result.Principal.FindFirstValue(ClaimTypes.Role));
    }

    private sealed class TestOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue => currentValue;
        public T Get(string? name) => currentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
