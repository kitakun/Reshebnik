using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Tabligo.SignalR.Hubs;

[Authorize]
public class JobOperationHub : Hub
{
    public async Task JoinCompanyGroup(int companyId)
    {
        // Get company ID from user claims
        var userCompanyId = Context.User?.FindFirst("company")?.Value;
        
        if (userCompanyId == null || int.Parse(userCompanyId) != companyId)
        {
            throw new UnauthorizedAccessException("You do not have access to this company's notifications");
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId}");
    }

    public async Task LeaveCompanyGroup(int companyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company_{companyId}");
    }
}
