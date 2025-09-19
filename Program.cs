using Reshebnik.Web.ProgramExtensions;
using Reshebnik.Web;
using Reshebnik.Web.Middleware;
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

#if RELEASE
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("certificate.pfx", "f5432yx5o");
    });
});
#endif

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

#if RELEASE
app.UseHttpsRedirection();
#endif

app.MapGet("/", () => "❤️");

await app.RunAsync();