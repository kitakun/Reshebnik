using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.JobOperation;

public class JobOperationStatusResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JobOperationStatusEnum Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public object? InputData { get; set; }
    public object? ResultData { get; set; }
    public int RetryCount { get; set; }
}
