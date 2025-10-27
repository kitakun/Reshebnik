using Tabligo.Domain.Enums;
using System.Text.Json;

namespace Tabligo.Domain.Entities;

public class JobOperationEntity
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public required string Type { get; set; }
    public int Hash { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public JobOperationStatusEnum Status { get; set; }
    public int RetryCount { get; set; }
    public JsonDocument? InputData { get; set; }
    public JsonDocument? Data { get; set; }

    public CompanyEntity Company { get; set; } = null!;
}
