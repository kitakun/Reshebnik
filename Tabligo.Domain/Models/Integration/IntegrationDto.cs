using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Integration;

public class IntegrationDto
{
    public int Id { get; set; }
    public IntegrationTypeEnum Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActivated { get; set; }
    public bool IsAvailable { get; set; }
    public string? ConfigurationStatus { get; set; }
}

public class IntegrationListResponse
{
    public List<IntegrationDto> Integrations { get; set; } = new();
}

public class IntegrationSearchRequest
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool? IsActivated { get; set; }
    public bool? IsAvailable { get; set; }
}

public class IntegrationSearchResponse
{
    public List<IntegrationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}