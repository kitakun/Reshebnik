using Tabligo.EntityFramework;
using Tabligo.Domain.Entities;
using Tabligo.Handlers.Auth;
using Tabligo.Handlers.Company;

using System.Diagnostics;
using System.Text.Json;

namespace Tabligo.Web.Middleware;

public class ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger, IConfiguration configuration)
{
    private readonly bool _includeStackTrace = configuration.GetValue<bool>("ErrorHandling:IncludeStackTrace", false);
    private readonly bool _includeInnerException = configuration.GetValue<bool>("ErrorHandling:IncludeInnerException", true);

    [DebuggerHidden]
    public async Task InvokeAsync(HttpContext context, TabligoContext db, UserContextHandler userContext, CompanyContextHandler companyContext)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await LogExceptionAsync(ex, context, db, userContext, companyContext);
            await HandleExceptionResponseAsync(context, ex);
        }
    }

    private async Task LogExceptionAsync(Exception ex, HttpContext context, TabligoContext db, UserContextHandler userContext, CompanyContextHandler companyContext)
    {
        string? email = null;
        int? companyId = null;
        try
        {
            var user = await userContext.GetCurrentUserAsync(context.RequestAborted);
            email = user.Email;
            companyId = user.CompanyId;
        }
        catch
        {
            try
            {
                companyId = await companyContext.CurrentCompanyIdAsync;
            }
            catch
            {
                // ignore
            }
        }

        // Only log exception if we have a valid company_id or if the company_id is not required
        if (companyId.HasValue)
        {
            db.ExceptionLogs.Add(new LogExceptionEntity
            {
                Message = ex.Message,
                StackTrace = ex.ToString(),
                UserEmail = email,
                CompanyId = companyId
            });
            await db.SaveChangesAsync(context.RequestAborted);
        }
        logger.LogError(ex, "Unhandled exception logged for user {Email} in company {CompanyId}", email, companyId);
    }

    private async Task HandleExceptionResponseAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = new
            {
                message = ex.Message,
                type = ex.GetType().Name,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                traceId = context.TraceIdentifier,
                stackTrace = _includeStackTrace ? ex.StackTrace : null,
                innerException = _includeInnerException && ex.InnerException != null ? new
                {
                    message = ex.InnerException.Message,
                    type = ex.InnerException.GetType().Name,
                    stackTrace = _includeStackTrace ? ex.InnerException.StackTrace : null
                } : null
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}

public static class ExceptionLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionLoggingMiddleware>();
    }
}
