using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;
using Reshebnik.Handlers.Company;
using Reshebnik.Handlers.Department;
using Reshebnik.Handlers.Employee;
using Reshebnik.Handlers.Structure;
using Reshebnik.Handlers.Metric;
using Reshebnik.Handlers.Indicator;
using Reshebnik.Handlers.KeyIndicator;
using Reshebnik.Handlers.IndicatorCategory;
using Reshebnik.Handlers.Dashboard;
using Reshebnik.Clickhouse;
using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Handlers;
using Reshebnik.Handlers.Email;
using Reshebnik.Handlers.SpecialInvitation;
using Reshebnik.Handlers.BugHunt;
using Reshebnik.Web.Middleware;
using Reshebnik.Web;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true
    )
    .AddEnvironmentVariables();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new Reshebnik.Web.Converters.DateTimeUtcConverter());
    options.JsonSerializerOptions.Converters.Add(new Reshebnik.Web.Converters.NullableDateTimeUtcConverter());
});
builder.Services.ConfigureHttpJsonOptions(_ => { });

// DB
builder.Services.AddDbContext<ReshebnikContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// AuthTokens
var secretKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKey123!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ReshebnikApp";
var key = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
        IdentityModelEventSource.ShowPII = true;
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception}");
                return Task.CompletedTask;
            }
        };
    });

// HttpRequests
builder.Services.AddResponseCaching();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
#if DEBUG
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        policy
            .WithOrigins("https://tabligo.ru")
            .AllowAnyHeader()
            .AllowAnyMethod();
#else
        policy
            .WithOrigins("https://tabligo.ru")
            .AllowAnyHeader()
            .AllowAnyMethod();
#endif
    });
});

// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("Client", new OpenApiInfo { Title = "Client API", Version = "v1" });
    options.SwaggerDoc("Super", new OpenApiInfo { Title = "Super API", Version = "v1" });

    // Optional: remove default grouping by controller name
    options.TagActionsBy(api =>
    {
        var groupName = api.GroupName;
        return [groupName ?? api.ActionDescriptor.RouteValues["controller"]];
    });
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        var groupName = apiDesc.GroupName;
        return groupName == docName;
    });

    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new List<string>()
        }
    });
});

// services
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<CreateJwtHandler>();
builder.Services.AddSingleton<SecurityHandler>();
builder.Services.AddScoped<UserContextHandler>();
builder.Services.AddScoped<CompanyContextHandler>();
builder.Services.AddScoped<AuthLoginHandler>();
builder.Services.AddScoped<AuthInviteHandler>();
builder.Services.AddScoped<AuthGetInviteHandler>();

builder.Services.AddScoped<DepartmentGetHandler>();
builder.Services.AddScoped<DepartmentPreviewHandler>();
builder.Services.AddScoped<DepartmentPutHandler>();
builder.Services.AddScoped<DepartmentGetByIdHandler>();
builder.Services.AddScoped<DepartmentPutOneHandler>();
builder.Services.AddScoped<DepartmentDeleteHandler>();
builder.Services.AddScoped<DepartmentFormGetByIdHandler>();
builder.Services.AddScoped<DepartmentFormPutHandler>();
builder.Services.AddScoped<EmployeesGetHandler>();
builder.Services.AddScoped<EmployeesTypeaheadHandler>();
builder.Services.AddScoped<DepartmentTypeaheadHandler>();
builder.Services.AddScoped<EmployeeGetByIdHandler>();
builder.Services.AddScoped<EmployeePutHandler>();
builder.Services.AddScoped<EmployeeDeleteHandler>();
builder.Services.AddScoped<EmployeeCommentUpdateHandler>();
builder.Services.AddScoped<CompanyUpdateHandler>();
builder.Services.AddScoped<CompanyGetHandler>();
builder.Services.AddScoped<SuCompanyGetHandler>();
builder.Services.AddScoped<CompanySettingsUpdateHandler>();
builder.Services.AddScoped<SuCompanySettingsUpdateHandler>();
builder.Services.AddScoped<StructureGetHandler>();
builder.Services.AddScoped<StructurePutHandler>();
builder.Services.AddScoped<MetricGetHandler>();
builder.Services.AddScoped<MetricPutHandler>();
builder.Services.AddScoped<IndicatorGetHandler>();
builder.Services.AddScoped<IndicatorPutHandler>();
builder.Services.AddScoped<IndicatorTypeaheadHandler>();
builder.Services.AddScoped<KeyIndicatorGetHandler>();
builder.Services.AddScoped<IndicatorCategoryGetHandler>();
builder.Services.AddScoped<IndicatorCategoryCommentUpdateHandler>();
builder.Services.Configure<ClickhouseOptions>(builder.Configuration.GetSection("Clickhouse"));
builder.Services.AddScoped<FetchUserMetricsHandler>();
builder.Services.AddScoped<FetchCompanyMetricsHandler>();
builder.Services.AddScoped<FetchDepartmentCompletionHandler>();
builder.Services.AddScoped<UserPreviewMetricsHandler>();
builder.Services.AddScoped<UserPreviewMetricsPutHandler>();
builder.Services.AddScoped<CompanyPreviewMetricsHandler>();
builder.Services.AddScoped<CompanyPreviewMetricsPutHandler>();
builder.Services.AddScoped<MigrateClickhouseDatabase>();
builder.Services.AddScoped<DashboardGetHandler>();
builder.Services.AddScoped<BugHuntCreateHandler>();
builder.Services.AddScoped<DepartmentEmployeesUpsertHandler>();
builder.Services.AddScoped<WelcomeHandler>();

// SU
builder.Services.AddScoped<SuTypeaheadCompaniesHandler>();
builder.Services.AddScoped<SuAllCompanyIdsHandler>();
builder.Services.AddScoped<SpecialInvitationCreateHandler>();
builder.Services.AddScoped<SuSpecialInvitationTypeaheadHandler>();
builder.Services.AddScoped<SuSpecialInvitationAcceptHandler>();
builder.Services.AddScoped<SuSpecialInvitationRejectHandler>();
builder.Services.AddScoped<SuBugHuntTypeaheadHandler>();

// Email
builder.Services.AddScoped<IEmailQueue, EfEmailQueue>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<EmailSenderService>();

var app = builder.Build();
TimeZoneHelper.HttpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
app.UseCors("DevCors");
app.UseExceptionLogging();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReshebnikContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReshebnikContext>>();
    logger.LogInformation("before database migration");
    await db.Database.MigrateAsync(); // ⬅️ Applies any pending migrations
    logger.LogInformation("migrated");
}

using (var scope = app.Services.CreateScope())
{
    var clickhouse = scope.ServiceProvider.GetRequiredService<MigrateClickhouseDatabase>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReshebnikContext>>();
    logger.LogInformation("before clickhouse migration");
    await clickhouse.HandleAsync(); // ⬅️ Applies any pending migrations
    logger.LogInformation("migrated");
}

// if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/Client/swagger.json", "Client API"); });
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/Super/swagger.json", "Super API"); });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

#if RELEASE
        app.UseHttpsRedirection();
#endif
app.MapGet("/", () => "❤️");

await app.RunAsync();