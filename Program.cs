using CMSApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using CMSApi.Data.Repository;
using CMSApi.Data;
using CMSApi.Authentication;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Add services --------------------

// Controllers + JSON serialization options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent JSON reference cycles (Versions -> CmsEntity -> Versions ...)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 32; // optimized for performance
    });

// EF Core DbContexts (write + read-only)
builder.Services.AddDbContext<ApplicationDbContext>(options => ConfigureDbProvider(options, builder.Configuration));

builder.Services.AddDbContext<ReadOnlyApplicationDbContext>(options =>
{
    ConfigureDbProvider(options, builder.Configuration);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Repository DI
builder.Services.AddScoped<ICmsEntityRepository, CmsEntityRepository>();
builder.Services.AddScoped<ICmsEntityVersionRepository, CmsEntityVersionRepository>();

// Service DI
builder.Services.AddScoped<ICmsEntityService, CmsEntityService>();
builder.Services.AddScoped<IEntityService, EntityService>();

// Bind BasicAuth configuration
builder.Services.Configure<BasicAuthOptions>(
    builder.Configuration.GetSection("BasicAuth")
);

// Basic Authentication
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationService>("BasicAuthentication", null);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CmsIngestPolicy", policy =>
        policy.RequireRole("CmsIngest"))
    .AddPolicy("ApiReadPolicy", policy =>
        policy.RequireRole("ApiUser", "Admin"))
    .AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("Admin"));

var app = builder.Build();

// -------------------- Middleware --------------------
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();

static void ConfigureDbProvider(DbContextOptionsBuilder options, IConfiguration configuration)
{
    var provider = configuration["DatabaseProvider"] ?? "SqlServer";

    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        var sqliteConnection = configuration.GetConnectionString("SqliteConnection")
            ?? "Data Source=cmsapi.dev.db";
        options.UseSqlite(sqliteConnection);
    }
    else
    {
        var sqlServerConnection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        options.UseSqlServer(sqlServerConnection);
    }
}