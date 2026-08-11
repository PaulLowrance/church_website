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

builder.AddServiceDefaults();

// Audio upload limits (Kestrel + multipart form). Keys off Podcast:MaxAudioBytes
// to keep the request-size ceiling consistent with the per-file validator.
var maxAudioBytes = int.TryParse(builder.Configuration["Podcast:MaxAudioBytes"], out var b) && b > 0
    ? b
    : 524_288_000;
builder.WebHost.ConfigureKestrel(opts => opts.Limits.MaxRequestBodySize = maxAudioBytes);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = maxAudioBytes;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});

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

// Ensure storage paths are absolute before registering services
var uploadsPath = builder.Configuration["Storage:AudioPath"] ?? "uploads/audio";
if (!Path.IsPathRooted(uploadsPath))
{
    uploadsPath = Path.Combine(builder.Environment.ContentRootPath, uploadsPath);
    builder.Configuration["Storage:AudioPath"] = uploadsPath;
}

var imagesPath = builder.Configuration["Storage:ImagesPath"] ?? "uploads/images";
if (!Path.IsPathRooted(imagesPath))
{
    imagesPath = Path.Combine(builder.Environment.ContentRootPath, imagesPath);
    builder.Configuration["Storage:ImagesPath"] = imagesPath;
}

var transcriptsPath = builder.Configuration["Storage:TranscriptPath"] ?? "uploads/transcripts";
if (!Path.IsPathRooted(transcriptsPath))
{
    transcriptsPath = Path.Combine(builder.Environment.ContentRootPath, transcriptsPath);
    builder.Configuration["Storage:TranscriptPath"] = transcriptsPath;
}

// Infrastructure services (DB, repositories, auth)
var connectionString = builder.Configuration.GetConnectionString("churchwebsite")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddInfrastructure(connectionString);

// FastEndpoints
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.MapDefaultEndpoints();

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

// Serve uploaded images
var imagesStaticFilesPath = app.Configuration["Storage:ImagesPath"] ?? "uploads/images";
Directory.CreateDirectory(imagesStaticFilesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagesStaticFilesPath),
    RequestPath = "/uploads/images"
});

// Serve generated transcripts
var transcriptsStaticFilesPath = app.Configuration["Storage:TranscriptPath"] ?? "uploads/transcripts";
Directory.CreateDirectory(transcriptsStaticFilesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(transcriptsStaticFilesPath),
    RequestPath = "/uploads/transcripts"
});

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
