using Microsoft.AspNetCore.SignalR;
using Tabligo.Domain.Services;
using Tabligo.SignalR.Hubs;

namespace Tabligo.SignalR.Services;

public class SignalRNotifier(IHubContext<JobOperationHub> hubContext) : INotifier
{
    public async Task NotifyJobStatusChangedAsync(int jobId, int companyId, string status, CancellationToken ct = default)
    {
        await hubContext.Clients
            .Group($"company_{companyId}")
            .SendAsync("JobStatusChanged", new { jobId, companyId, status }, ct);
    }
}

