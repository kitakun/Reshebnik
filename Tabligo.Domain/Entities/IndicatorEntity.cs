using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Entities;

public class IndicatorEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public IndicatorUnitTypeEnum UnitType { get; set; }
    public FillmentPeriodEnum FillmentPeriod { get; set; }
    public IndicatorValueTypeEnum ValueType { get; set; }

    public decimal RejectionTreshold { get; set; }

    public bool ShowToEmployees { get; set; }
    public bool ShowOnMainScreen { get; set; }
    public bool ShowOnKeyIndicators { get; set; }

    public decimal? Plan { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }

    public int? EmployeeId { get; set; }
    public EmployeeEntity? Employee { get; set; }

    public int? DepartmentId { get; set; }
    public DepartmentEntity? Department { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }
    public CompanyEntity CreatedByCompany { get; set; } = null!;

    public bool IsArchived { get; set; }
    public int? ArchiveMetricId { get; set; }
    public ArchivedMetricEntity? ArchivedMetric { get; set; }
    
    public List<ExternalIdLinkEntity> ExternalIdLinks { get; set; } = new();
}

