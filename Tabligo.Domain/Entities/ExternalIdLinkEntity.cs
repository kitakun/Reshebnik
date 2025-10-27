using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Entities;

public class ExternalIdLinkEntity
{
    public int Id { get; set; }
    
    // Company relationship
    public int CompanyId { get; set; }
    public CompanyEntity Company { get; set; } = null!;
    
    // Core properties
    public string ExternalId { get; set; } = null!;
    public IntegrationTypeEnum IntegrationType { get; set; }
    
    // Polymorphic fields for indexing and querying
    public string EntityType { get; set; } = null!;
    public int EntityId { get; set; }
    
    // Direct navigation properties for type safety
    public int? EmployeeId { get; set; }
    public EmployeeEntity? Employee { get; set; }
    
    public int? DepartmentId { get; set; }
    public DepartmentEntity? Department { get; set; }
    
    public int? MetricId { get; set; }
    public MetricEntity? Metric { get; set; }
    
    public int? IndicatorId { get; set; }
    public IndicatorEntity? Indicator { get; set; }
}
