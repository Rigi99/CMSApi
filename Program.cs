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

// EF Core DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Repository DI
builder.Services.AddScoped<ICmsEntityRepository, CmsEntityRepository>();
builder.Services.AddScoped<ICmsEntityVersionRepository, CmsEntityVersionRepository>();

// Service DI
builder.Services.AddScoped<ICmsEntityService, CmsEntityService>();
builder.Services.AddScoped<IEntityService, EntityService>();

// Basic Authentication
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationService>("BasicAuthentication", null);

builder.Services.AddAuthorization();

var app = builder.Build();

// -------------------- Middleware --------------------
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();