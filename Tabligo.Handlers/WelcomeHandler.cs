using Microsoft.EntityFrameworkCore;

using Tabligo.EntityFramework;
using Tabligo.Handlers.Auth;

namespace Tabligo.Handlers;

public class WelcomeHandler(UserContextHandler userContextHandler, TabligoContext TabligoContext)
{
    public async ValueTask<bool> HandleGetStatus()
    {
        var user = await userContextHandler.GetCurrentUserAsync();
        return user.WelcomeWasSeen ?? false;
    }

    public async ValueTask HandleSetSeen()
    {
        var user = await TabligoContext.Employees.FirstAsync(f => f.Id == userContextHandler.CurrentUserId);
        user.WelcomeWasSeen = true;
        await TabligoContext.SaveChangesAsync();
    }
}