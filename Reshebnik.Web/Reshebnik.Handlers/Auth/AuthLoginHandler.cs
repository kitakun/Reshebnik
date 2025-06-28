using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Reshebnik.Domain.Models;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Auth;

public class AuthLoginHandler(
    ReshebnikContext context,
    CompanyContextHandler companyContext,
    IConfiguration configuration,
    SecurityHandler securityHandler,
    CreateJwtHandler jwtHandler)
{
    public async ValueTask<CreateJwtHandler.JwtResponseRecord?> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await context.Employees.FirstOrDefaultAsync(f => f.Email.ToLower() == request.Email.ToLower(), cancellationToken);
        if (existingUser == null) return null;

        var isCorrect = securityHandler.VerifyPassword(request.Password, existingUser.Salt, existingUser.Password);
        if (!isCorrect || !existingUser.IsActive) return null;

        existingUser.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        var curCompany = await companyContext.CurrentCompanyAsync;
        return jwtHandler.CreateToken(existingUser, configuration, curCompany?.Id);
    }
}