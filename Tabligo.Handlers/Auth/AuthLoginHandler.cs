using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Auth;

public class AuthLoginHandler(
    TabligoContext context,
    CompanyContextHandler companyContext,
    SecurityHandler securityHandler,
    CreateJwtHandler jwtHandler)
{
    public async ValueTask<CreateJwtHandler.JwtResponseRecord?> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return null;

        var existingUser = await context
            .Employees
            .Include(i => i.Company)
            .FirstOrDefaultAsync(f => f.Email != null && f.Email.ToLower() == request.Email.ToLower(), cancellationToken);
        if (existingUser == null) return null;

        var isCorrect = securityHandler.VerifyPassword(request.Password, existingUser.Salt, existingUser.Password);
        if (!isCorrect || !existingUser.IsActive) return null;

        existingUser.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        var curCompany = await companyContext.CurrentCompanyAsync;
        return jwtHandler.CreateToken(existingUser, curCompany?.Id ?? existingUser.CompanyId);
    }
}