using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Auth;

public class AuthInviteHandler(
    ReshebnikContext context,
    CompanyContextHandler companyContext,
    SecurityHandler securityHandler,
    CreateJwtHandler jwtHandler,
    IConfiguration configuration)
{
    public async ValueTask<CreateJwtHandler.JwtResponseRecord?> HandleAsync(string code, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password)) return null;

        var existingUser = await context.Employees.FirstOrDefaultAsync(f => f.EmailInvitationCode!.ToLower() == code.ToLower(), cancellationToken);
        if (existingUser == null || !existingUser.IsActive) return null;

        existingUser.Password = securityHandler.GenerateSaltedHash(password, out var generatedSalt);
        existingUser.Salt = generatedSalt;
        existingUser.EmailInvitationCode = string.Empty;

        existingUser.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        var curCompany = await companyContext.CurrentCompanyAsync;
        return jwtHandler.CreateToken(existingUser, configuration, curCompany?.Id);
    }
}
