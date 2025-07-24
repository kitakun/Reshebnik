using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Reshebnik.EntityFramework;
using Reshebnik.Domain.Entities;
using Reshebnik.Handlers.Auth;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Web.Middleware;

public class ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, ReshebnikContext db, UserContextHandler userContext, CompanyContextHandler companyContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
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

            db.ExceptionLogs.Add(new LogExceptionEntity
            {
                Message = ex.Message,
                StackTrace = ex.ToString(),
                UserEmail = email,
                CompanyId = companyId
            });
            await db.SaveChangesAsync(context.RequestAborted);
            _logger.LogError(ex, "Unhandled exception logged");
            throw;
        }
    }
}

public static class ExceptionLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionLoggingMiddleware>();
    }
}
