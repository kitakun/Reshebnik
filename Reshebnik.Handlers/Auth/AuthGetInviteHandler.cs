using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Auth;

public class AuthGetInviteHandler(ReshebnikContext context)
{
    // ReSharper disable once MemberCanBePrivate.Global
    public readonly record struct InviteInfoRecord(string Name, string Email);

    public async ValueTask<InviteInfoRecord?> HandleAsync(string code, CancellationToken cancellationToken = default)
    {
        var existingUser = await context.Employees.FirstOrDefaultAsync(f => f.EmailInvitationCode!.ToLower() == code.ToLower(), cancellationToken);
        if (existingUser == null || !existingUser.IsActive) return null;

        existingUser.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return new InviteInfoRecord(existingUser.FIO, existingUser.Email ?? string.Empty);
    }
}
