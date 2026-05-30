using ChurchWebsite.Infrastructure;
using ChurchWebsite.Infrastructure.Data;
using Dapper;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Enable snake_case to PascalCase mapping for Dapper
DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

// JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ChurchWebsite";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ChurchWebsiteUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS for Vite dev server proxy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Ensure storage path is absolute before registering services
var uploadsPath = builder.Configuration["Storage:AudioPath"] ?? "uploads/audio";
if (!Path.IsPathRooted(uploadsPath))
{
    uploadsPath = Path.Combine(builder.Environment.ContentRootPath, uploadsPath);
    builder.Configuration["Storage:AudioPath"] = uploadsPath;
}

// Infrastructure services (DB, repositories, auth)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddInfrastructure(connectionString);

// FastEndpoints
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

// Initialize database schema and seed data
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

app.UseCors("AllowVite");
app.UseAuthentication();
app.UseAuthorization();

// Serve uploaded audio files
var staticFilesPath = app.Configuration["Storage:AudioPath"] ?? "uploads/audio";
Directory.CreateDirectory(staticFilesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(staticFilesPath),
    RequestPath = "/uploads/audio"
});

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
