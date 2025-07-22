using System.Text.Json;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Models.BugHunt;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.BugHunt;

public class BugHuntCreateHandler(ReshebnikContext db)
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
