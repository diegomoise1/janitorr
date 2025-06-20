using JanitorAspNet.Configuration;
using JanitorAspNet.Clients;
using JanitorAspNet.Services;
using JanitorAspNet.Webhooks;
using Refit;
using Serilog;

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

// Configure webhook options and service
builder.Services.Configure<WebhookOptions>(
    builder.Configuration.GetSection("Application:Webhooks"));

builder.Services.AddHttpClient<IWebhookService, WebhookService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();

// Add application services
builder.Services.AddScoped<ICleanupService, CleanupService>();

// Add background service for scheduled cleanup
builder.Services.AddHostedService<CleanupBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
