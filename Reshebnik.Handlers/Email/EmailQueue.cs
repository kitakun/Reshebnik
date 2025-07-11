using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Email;

public interface IEmailQueue
{
    Task EnqueueAsync(EmailMessageEntity message);
    Task<EmailMessageEntity?> DequeueAsync(CancellationToken cancellationToken);
}

public class EfEmailQueue(ReshebnikContext db) : IEmailQueue
{
    private TaskCompletionSource<bool> _delayTcs = CreateDelayTcs();

    private static TaskCompletionSource<bool> CreateDelayTcs()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    
    public async Task EnqueueAsync(EmailMessageEntity message)
    {
        db.EmailQueue.Add(message);
        await db.SaveChangesAsync();
        _delayTcs.TrySetResult(true);
    }

    public async Task<EmailMessageEntity?> DequeueAsync(CancellationToken cancellationToken)
    {
        EmailMessageEntity? next = await db.EmailQueue
            .AsNoTracking()
            .Where(e => !e.IsSent && e.Error == null)
            .OrderBy(e => e.EnqueuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (next == null)
        {
            var delayTask = Task.Delay(10 * 1000, cancellationToken);
            var tcsTask = _delayTcs.Task;

            var completedTask = await Task.WhenAny(delayTask, tcsTask);

            if (completedTask == tcsTask)
            {
                _delayTcs = CreateDelayTcs();
            }
        }

        return next;
    }
}