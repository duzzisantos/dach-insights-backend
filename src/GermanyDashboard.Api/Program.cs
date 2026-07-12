using System.Threading.RateLimiting;
using GermanyDashboard.Api.Middleware;
using GermanyDashboard.Infrastructure;
using GermanyDashboard.Infrastructure.ExternalServices.Destatis;
using GermanyDashboard.Infrastructure.ExternalServices.Eurostat;
using GermanyDashboard.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddResponseCaching();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing ConnectionStrings:Default. Set ConnectionStrings__Default.");
}

builder.Services.AddHealthChecks().AddNpgSql(connectionString);

// CORS: only the configured frontend origin(s) may call this API, and only GET/HEAD —
// this API has no mutating endpoints, so wildcard methods/headers are never needed.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Missing Cors:AllowedOrigins configuration.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "HEAD")
            .AllowAnyHeader();
    });
});

// Rate limiting: a global fixed window per client, keyed by IP, to blunt scraping/abuse
// against a public read-only API with no auth in front of it.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 120,
            QueueLimit = 0,
        });
    });
});

var app = builder.Build();

if (args.Contains("seed"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DbMigrator>().MigrateAndSeedAsync();
    return;
}

if (args.Contains("sync-destatis"))
{
    using var scope = app.Services.CreateScope();
    var sync = scope.ServiceProvider.GetRequiredService<DestatisSyncService>();
    // Map GENESIS table codes to indicator slugs here once you know which tables your
    // Destatis account has access to, e.g.: ["12411-0010"] = "population"
    await sync.SyncAsync(new Dictionary<string, string>());
    return;
}

if (args.Contains("sync-eurostat"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DbMigrator>().MigrateAndSeedAsync();
    await scope.ServiceProvider.GetRequiredService<EurostatSyncService>().SyncAllAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar/v1 (dev only)
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DbMigrator>().MigrateAndSeedAsync();
}

app.UseExceptionHandler();
app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseResponseCaching();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
