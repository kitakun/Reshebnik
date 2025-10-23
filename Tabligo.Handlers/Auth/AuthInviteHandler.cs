using Microsoft.EntityFrameworkCore;

using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Auth;

public class AuthInviteHandler(
    TabligoContext context,
    CompanyContextHandler companyContext,
    SecurityHandler securityHandler,
    CreateJwtHandler jwtHandler)
{
    public async ValueTask<CreateJwtHandler.JwtResponseRecord?> HandleAsync(string code, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password)) return null;

        var existingUser = await context
            .Employees
            .Include(i => i.Company)
            .FirstOrDefaultAsync(f => f.EmailInvitationCode!.ToLower() == code.ToLower(), cancellationToken);
        if (existingUser is not { IsActive: true }) return null;

        existingUser.Password = securityHandler.GenerateSaltedHash(password, out var generatedSalt);
        existingUser.Salt = generatedSalt;
        existingUser.EmailInvitationCode = string.Empty;

        existingUser.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        var curCompany = await companyContext.CurrentCompanyAsync;
        return jwtHandler.CreateToken(existingUser, curCompany?.Id ?? existingUser.CompanyId);
    }
}