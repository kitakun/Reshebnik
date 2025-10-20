using Reshebnik.Web.ProgramExtensions;
using Reshebnik.Web;
using Reshebnik.Web.Middleware;
using Reshebnik.Web.Reshebnik.Web;

using ZLogger;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure ZLogger with proper formatters
builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole();
// builder.Logging.AddZLoggerFile("logs/app.log");

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true
    )
    .AddEnvironmentVariables();

// Configure services
builder.Services.AddReshebnikHttpConfigurations();
builder.Services.AddReshebnikServices(builder.Configuration);
builder.Services.AddReshebnikAuthentication(builder.Configuration);
builder.Services.AddReshebnikCors();
builder.Services.AddReshebnikSwagger();

// Configure health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["self"]);

// Configure Kestrel based on environment
if (builder.Environment.IsProduction())
{
    // Production: Try to use custom certificate, fall back to development certificate
    builder.WebHost.ConfigureKestrel(options =>
    {
        if (File.Exists("certificate.pfx"))
        {
            var certificatePassword = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");
            if (string.IsNullOrEmpty(certificatePassword))
            {
                throw new InvalidOperationException("CERTIFICATE_PASSWORD environment variable is required for production deployment");
            }
            
            options.ListenAnyIP(443, listenOptions =>
            {
                listenOptions.UseHttps("certificate.pfx", certificatePassword);
            });
        }
        else
        {
            // Fall back to development certificate or HTTP
            Console.WriteLine("Warning: certificate.pfx not found, using development certificate");
            options.ListenAnyIP(443, listenOptions =>
            {
                listenOptions.UseHttps();
            });
        }
    });
}
else
{
    // Development: Use development certificate or HTTP only
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000); // HTTP
        // Try to use development certificate, fall back to HTTP only if it fails
        try
        {
            options.ListenAnyIP(5001, listenOptions =>
            {
                listenOptions.UseHttps(); // Development certificate
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not configure HTTPS: {ex.Message}");
            Console.WriteLine("Running in HTTP-only mode for development");
        }
    });
}

var app = builder.Build();

// Configure HTTP context accessor
TimeZoneHelper.HttpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();

// Configure middleware pipeline
app.UseCors("DevCors");
app.UseExceptionLogging();

// Run migrations if enabled
await app.RunMigrationsAsync(builder.Configuration);

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/Client/swagger.json", "Client API"); });
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/Super/swagger.json", "Super API"); });

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration.TotalMilliseconds,
                Exception = entry.Value.Exception?.Message,
                Data = entry.Value.Data
            }),
            Duration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("self") || check.Tags.Contains("database")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Only check if the application is running
});

// Enable HTTPS redirection in production
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => "❤️");

await app.RunAsync();