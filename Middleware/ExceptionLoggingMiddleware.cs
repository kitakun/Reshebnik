using Reshebnik.EntityFramework;
using Reshebnik.Domain.Entities;
using Reshebnik.Handlers.Auth;
using Reshebnik.Handlers.Company;

using System.Diagnostics;

namespace Reshebnik.Web.Middleware;

public class ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
{
    [DebuggerHidden]
    public async Task InvokeAsync(HttpContext context, ReshebnikContext db, UserContextHandler userContext, CompanyContextHandler companyContext)
    {
        try
        {
            await next(context);
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
            logger.LogError(ex, "Unhandled exception logged");
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
