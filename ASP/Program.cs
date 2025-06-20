using JanitorAspNet.Configuration;
using JanitorAspNet.Clients;
using JanitorAspNet.Services;
using JanitorAspNet.Webhooks;
using JanitorAspNet.HealthChecks;
using JanitorAspNet.Middleware;
using Refit;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure application options
builder.Services.Configure<ApplicationOptions>(
    builder.Configuration.GetSection(ApplicationOptions.SectionName));

// Configure HTTP clients with Refit
builder.Services.AddHttpClient();

// Configure Jellyseerr client
builder.Services.AddRefitClient<IJellyseerrClient>()
    .ConfigureHttpClient((serviceProvider, httpClient) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApplicationOptions>>();
        var jellyseerr = options.Value.Jellyseerr;
        
        if (jellyseerr.Enabled && !string.IsNullOrEmpty(jellyseerr.Url))
        {
            httpClient.BaseAddress = new Uri(jellyseerr.Url);
            if (!string.IsNullOrEmpty(jellyseerr.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", jellyseerr.ApiKey);
            }
        }
    });

// Configure Radarr client
builder.Services.AddRefitClient<IRadarrClient>()
    .ConfigureHttpClient((serviceProvider, httpClient) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApplicationOptions>>();
        var radarr = options.Value.Radarr;
        
        if (radarr.Enabled && !string.IsNullOrEmpty(radarr.Url))
        {
            httpClient.BaseAddress = new Uri($"{radarr.Url}/api/v3");
            if (!string.IsNullOrEmpty(radarr.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", radarr.ApiKey);
            }
        }
    });

// Configure Sonarr client
builder.Services.AddRefitClient<ISonarrClient>()
    .ConfigureHttpClient((serviceProvider, httpClient) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApplicationOptions>>();
        var sonarr = options.Value.Sonarr;
        
        if (sonarr.Enabled && !string.IsNullOrEmpty(sonarr.Url))
        {
            httpClient.BaseAddress = new Uri($"{sonarr.Url}/api/v3");
            if (!string.IsNullOrEmpty(sonarr.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", sonarr.ApiKey);
            }
        }
    });

// Configure Jellyfin client
builder.Services.AddRefitClient<IJellyfinClient>()
    .ConfigureHttpClient((serviceProvider, httpClient) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApplicationOptions>>();
        var jellyfin = options.Value.Jellyfin;
        
        if (jellyfin.Enabled && !string.IsNullOrEmpty(jellyfin.Url))
        {
            httpClient.BaseAddress = new Uri($"{jellyfin.Url}");
            if (!string.IsNullOrEmpty(jellyfin.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-Emby-Token", jellyfin.ApiKey);
            }
        }
    });

// Configure Bazarr client
builder.Services.AddRefitClient<IBazarrClient>()
    .ConfigureHttpClient((serviceProvider, httpClient) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApplicationOptions>>();
        var bazarr = options.Value.Bazarr;
        
        if (bazarr.Enabled && !string.IsNullOrEmpty(bazarr.Url))
        {
            httpClient.BaseAddress = new Uri(bazarr.Url);
            if (!string.IsNullOrEmpty(bazarr.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-API-KEY", bazarr.ApiKey);
            }
        }
    });

// Configure memory cache and caching service
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

// Configure background task queue
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();

// Add application services
builder.Services.AddScoped<ICleanupService, CleanupService>();
builder.Services.AddScoped<IMediaServerService, JellyfinMediaServerService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IStatsService, JellystatStatsService>();
builder.Services.AddScoped<IJellyseerrService, JellyseerrService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<RadarrHealthCheck>("radarr")
    .AddCheck<SonarrHealthCheck>("sonarr")
    .AddCheck<JellyfinHealthCheck>("jellyfin")
    .AddCheck<JellyseerrHealthCheck>("jellyseerr")
    .AddCheck<FileSystemHealthCheck>("filesystem")
    .AddCheck<WebhookHealthCheck>("webhooks");

// Configure webhook options and service
builder.Services.Configure<WebhookOptions>(
    builder.Configuration.GetSection("Application:Webhooks"));

builder.Services.AddHttpClient<IWebhookService, WebhookService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();

// Add exception handling middleware
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

var app = builder.Build();

// Use exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add health check endpoint
app.MapHealthChecks("/health");

app.UseHttpsRedirection();

// Enable static files
app.UseStaticFiles();

// Add default route to dashboard
app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapGet("/dashboard", () => Results.Redirect("/index.html"));

app.UseAuthorization();
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Janitor ASP.NET application started");

app.Run();
