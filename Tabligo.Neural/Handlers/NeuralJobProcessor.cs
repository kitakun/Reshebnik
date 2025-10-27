using Microsoft.Extensions.Logging;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.Neural.Interfaces;
using System.Text.Json;
using static Tabligo.Domain.Models.JobOperation.JobOperationTypes;

namespace Tabligo.Neural.Handlers;

/// <summary>
/// Processes neural file job operations
/// </summary>
public class NeuralJobProcessor(
    ITabligoNeuralAgent neuralAgent,
    ILogger<NeuralJobProcessor> logger)
    : IJobOperationProcessor
{
    public string JobType => NeuralFileProcess;

    public async Task<object?> ProcessAsync(
        Domain.Entities.JobOperationEntity job,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (job.InputData == null)
        {
            throw new InvalidOperationException("Job does not contain input data");
        }

        var inputData = JsonSerializer.Deserialize<NeuralFileInputData>(
            job.InputData.RootElement.GetRawText(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (inputData == null)
        {
            throw new InvalidOperationException("Failed to deserialize input data");
        }

        logger.LogInformation("Processing neural file job {JobId} for file {FileName}", job.Id, inputData.FileName);

        var result = await neuralAgent.ProcessFileAsync(
            inputData.FileContent, 
            inputData.FileName, 
            cancellationToken);

        logger.LogInformation("Neural file job {JobId} completed successfully", job.Id);

        return result;
    }

    private class NeuralFileInputData
    {
        public string FileContent { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
