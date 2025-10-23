using System.Text.Json;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Models.BugHunt;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.BugHunt;

public class BugHuntCreateHandler(TabligoContext db)
{
    public async Task HandleAsync(BugHuntCreateRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new BugHuntEntity
        {
            Title = request.Title,
            Message = request.Message,
            Screenshot = request.Screenshot,
            LastRequestUrl = request.LastRequest?.Url,
            LastRequestResponse = request.LastRequest?.Response != null ? JsonSerializer.Serialize(request.LastRequest.Response) : null,
            LastRequestStatus = request.LastRequest?.Status,
            CreatedAt = DateTime.UtcNow
        };

        db.BugHunts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
