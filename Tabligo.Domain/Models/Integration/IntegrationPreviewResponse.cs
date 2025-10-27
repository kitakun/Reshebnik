namespace Tabligo.Domain.Models.Integration;

public class IntegrationPreviewResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<IntegrationImportRequest>? Items { get; set; }
    public int? JobId { get; set; }
}

