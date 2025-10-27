using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.EntityFramework;
using System.Text.Json;

namespace Tabligo.Handlers.JobOperation;

public class JobOperationGetHandler(TabligoContext db)
{
    public async Task<JobOperationEntity?> HandleAsync(int jobId, CancellationToken ct)
    {
        var job = await db.JobOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        return job;
    }

    public async Task<JobOperationStatusResponse> GetJobStatusAsync(int jobId, int companyId, CancellationToken ct)
    {
        var job = await db.JobOperations
            .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == companyId, ct);

        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} not found or does not belong to company {companyId}");
        }

        var response = new JobOperationStatusResponse
        {
            Id = job.Id,
            Type = job.Type,
            Name = job.Name,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            RetryCount = job.RetryCount
        };

        if (job.InputData != null)
        {
            var deserialized = JsonSerializer.Deserialize<JsonElement>(job.InputData.RootElement.GetRawText());
            response.InputData = MaskSensitiveData(deserialized);
        }

        if (job.Data != null)
        {
            var deserialized = JsonSerializer.Deserialize<JsonElement>(job.Data.RootElement.GetRawText());
            response.ResultData = MaskSensitiveData(deserialized);
        }

        return response;
    }

    private static object? MaskSensitiveData(JsonElement jsonElement)
    {
        if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, object?>();
            foreach (var property in jsonElement.EnumerateObject())
            {
                var key = property.Name;
                var value = property.Value;

                // Mask common API key field names
                if (IsSensitiveKey(key))
                {
                    result[key] = "***";
                }
                else if (value.ValueKind == JsonValueKind.Object)
                {
                    result[key] = MaskSensitiveData(value);
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    result[key] = value.EnumerateArray().Select(item => MaskSensitiveData(item)).ToArray();
                }
                else
                {
                    result[key] = GetValueAsObject(value);
                }
            }
            return result;
        }
        
        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            return jsonElement.EnumerateArray().Select(item => MaskSensitiveData(item)).ToArray();
        }

        return GetValueAsObject(jsonElement);
    }

    private static bool IsSensitiveKey(string key)
    {
        var lowerKey = key.ToLowerInvariant();
        return lowerKey.Contains("api") && lowerKey.Contains("key") ||
               lowerKey.Contains("password") ||
               lowerKey.Contains("secret") ||
               lowerKey.Contains("token") ||
               lowerKey == "apikey" ||
               lowerKey == "api_key" ||
               lowerKey == "apikey" ||
               lowerKey == "accesskey" ||
               lowerKey == "access_key";
    }

    private static object? GetValueAsObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}
