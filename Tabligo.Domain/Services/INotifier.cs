namespace Tabligo.Domain.Services;

public interface INotifier
{
    Task NotifyJobStatusChangedAsync(int jobId, int companyId, string status, CancellationToken ct = default);
}

