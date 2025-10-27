using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.JobOperation;

public class JobOperationStatusResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JobOperationStatusEnum Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public object? InputData { get; set; }
    public object? ResultData { get; set; }
    public int RetryCount { get; set; }
}

public class JobOperationSearchRequest
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public JobOperationStatusEnum? Status { get; set; }
    public string? Type { get; set; }
}

public class JobOperationSearchResponse
{
    public List<JobOperationStatusResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}