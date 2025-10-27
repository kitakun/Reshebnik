using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Entities;

public class IntegrationEntity
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public IntegrationTypeEnum Type { get; set; }
    public bool IsActivated { get; set; }
    public string? Configuration { get; set; } // JSONB for credentials/settings
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public CompanyEntity Company { get; set; } = null!;
}
