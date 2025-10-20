namespace Reshebnik.Domain.Models.Integration;

public class IntegrationImportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportError> Errors { get; set; } = new();
}

public class ImportError
{
    public string EntityType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
