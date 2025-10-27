using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.Domain.Services;

namespace Tabligo.Handlers.JobOperation;

public class JobOperationProcessorService(
    ILogger<JobOperationProcessorService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Job operation processor service is running");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var queue = scope.ServiceProvider.GetRequiredService<IJobOperationQueue>();
                var job = await queue.DequeueAsync(stoppingToken);

                if (job == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                await ProcessJobAsync(job, queue, scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing job queue");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("Job operation processor service is stopping");
    }

    private async Task ProcessJobAsync(Domain.Entities.JobOperationEntity job, IJobOperationQueue queue, IServiceProvider serviceProvider, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Processing job {JobId} of type {JobType}", job.Id, job.Type);

            // Get all processors from the scoped service provider
            var processors = serviceProvider.GetServices<IJobOperationProcessor>();
            var processorMap = processors.ToDictionary(p => p.JobType, p => p);

            if (!processorMap.TryGetValue(job.Type, out var processor))
            {
                logger.LogWarning("Unknown job type: {JobType}", job.Type);
                await queue.UpdateStatusAsync(job.Id, JobOperationStatusEnum.Failed, null, ct);
                var notifier1 = serviceProvider.GetRequiredService<INotifier>();
                await notifier1.NotifyJobStatusChangedAsync(job.Id, job.CompanyId, JobOperationStatusEnum.Failed.ToString(), ct);
                return;
            }

            var result = await processor.ProcessAsync(job, serviceProvider, ct);
            await queue.UpdateStatusAsync(job.Id, JobOperationStatusEnum.Finished, result, ct);
            var notifier = serviceProvider.GetRequiredService<INotifier>();
            await notifier.NotifyJobStatusChangedAsync(job.Id, job.CompanyId, JobOperationStatusEnum.Finished.ToString(), ct);
            logger.LogInformation("Job {JobId} completed successfully", job.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing job {JobId}", job.Id);
            
            if (job.RetryCount < MaxRetries)
            {
                job.RetryCount++;
                await queue.UpdateStatusAsync(job.Id, JobOperationStatusEnum.InQueue, null, ct);
                var notifier2 = serviceProvider.GetRequiredService<INotifier>();
                await notifier2.NotifyJobStatusChangedAsync(job.Id, job.CompanyId, JobOperationStatusEnum.InQueue.ToString(), ct);
                logger.LogInformation("Job {JobId} will be retried (attempt {RetryCount}/{MaxRetries})", job.Id, job.RetryCount, MaxRetries);
            }
            else
            {
                await queue.UpdateStatusAsync(job.Id, JobOperationStatusEnum.Failed, null, ct);
                var notifier3 = serviceProvider.GetRequiredService<INotifier>();
                await notifier3.NotifyJobStatusChangedAsync(job.Id, job.CompanyId, JobOperationStatusEnum.Failed.ToString(), ct);
                logger.LogError("Job {JobId} failed after {MaxRetries} retries", job.Id, MaxRetries);
            }
        }
    }
}
