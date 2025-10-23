using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Enums;
using Tabligo.Neural.Interfaces;

using System.Text.Json;

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

    private async Task ProcessJobAsync(Tabligo.Domain.Entities.JobOperationEntity job, IJobOperationQueue queue, IServiceProvider serviceProvider, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Processing job {JobId} of type {JobType}", job.Id, job.Type);

            switch (job.Type)
            {
                case "neural-file-process":
                    await ProcessNeuralFileJobAsync(job, queue, serviceProvider, ct);
                    break;
                default:
                    logger.LogWarning("Unknown job type: {JobType}", job.Type);
                    await queue.UpdateStatusAsync(job.Id, JobOperationStatusEnum.Failed, null, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing job {JobId}", job.Id);
            
            if (job.RetryCount < MaxRetries)
            {
                job.RetryCount++;
                await queue.UpdateStatusAsync(job.Id, JobOperationStatusEnum.InQueue, null, ct);
                logger.LogInformation("Job {JobId} will be retried (attempt {RetryCount}/{MaxRetries})", job.Id, job.RetryCount, MaxRetries);
            }
            else
            {
                await queue.UpdateStatusAsync(job.Id, JobOperationStatusEnum.Failed, null, ct);
                logger.LogError("Job {JobId} failed after {MaxRetries} retries", job.Id, MaxRetries);
            }
        }
    }

    private async Task ProcessNeuralFileJobAsync(Tabligo.Domain.Entities.JobOperationEntity job, IJobOperationQueue queue, IServiceProvider serviceProvider, CancellationToken ct)
    {
        var neuralAgent = serviceProvider.GetRequiredService<ITabligoNeuralAgent>();
        
        if (job.InputData == null)
        {
            throw new InvalidOperationException("Job does not contain input data");
        }

        var inputData = JsonSerializer.Deserialize<NeuralFileInputData>(job.InputData.RootElement.GetRawText());
        if (inputData == null)
        {
            throw new InvalidOperationException("Failed to deserialize input data");
        }

        var result = await neuralAgent.ProcessFileAsync(inputData.FileContent, inputData.FileName, ct);
        
        await queue.UpdateStatusAsync(job.Id, JobOperationStatusEnum.Finished, result, ct);
        logger.LogInformation("Job {JobId} completed successfully", job.Id);
    }

    private class NeuralFileInputData
    {
        public string FileContent { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
