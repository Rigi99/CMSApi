using CMSApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using CMSApi.Data.Repository;
using CMSApi.Data;
using CMSApi.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent JSON serialization cycles (Versions -> CmsEntity -> Versions ...)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

builder.Services.AddSwaggerGen();

// Configure EF Core DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Register repository
builder.Services.AddScoped<ICmsEntityRepository, CmsEntityRepository>();
builder.Services.AddScoped<ICmsEntityVersionRepository, CmsEntityVersionRepository>();

// Register service (depends on repository now)
builder.Services.AddScoped<ICmsEventService, CmsEventService>();
builder.Services.AddScoped<IEntityService, EntityService>();


// Basic Authentication
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationService>("BasicAuthentication", null);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

// Auth middleware
app.UseAuthentication();
app.UseAuthorization();

// Register controllers
app.MapControllers();

app.Run();