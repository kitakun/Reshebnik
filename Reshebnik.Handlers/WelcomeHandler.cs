using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;

namespace Reshebnik.Handlers;

public class WelcomeHandler(UserContextHandler userContextHandler, ReshebnikContext reshebnikContext)
{
    public async ValueTask<bool> HandleGetStatus()
    {
        var user = await userContextHandler.GetCurrentUserAsync();
        return user.WelcomeWasSeen ?? false;
    }

    public async ValueTask HandleSetSeen()
    {
        var user = await reshebnikContext.Employees.FirstAsync(f => f.Id == userContextHandler.CurrentUserId);
        user.WelcomeWasSeen = true;
        await reshebnikContext.SaveChangesAsync();
    }
}