using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Models.Indicator;

public class IndicatorPutDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = string.Empty;
    public IndicatorUnitTypeEnum UnitType { get; set; }
    public FillmentPeriodEnum FillmentPeriod { get; set; }
    public IndicatorValueTypeEnum ValueType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal RejectionTreshold { get; set; }
    public bool ShowToEmployees { get; set; }
    public bool ShowOnMainScreen { get; set; }
    public bool ShowOnKeyIndicators { get; set; }
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
}
