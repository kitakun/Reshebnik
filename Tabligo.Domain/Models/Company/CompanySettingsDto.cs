using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Company;

public class CompanySettingsDto
{
    public required string CompanyName { get; set; } = string.Empty;
    public required string Industry { get; set; } = string.Empty;
    public required string Size { get; set; } = string.Empty;
    public required CompanyTypeEnum LegalType { get; set; }
    public required string CompanyEmail { get; set; } = string.Empty;
    public required string CompanyPhone { get; set; } = string.Empty;
    public required PeriodTypeEnum Period { get; set; }
    public required string DefaultMetric { get; set; } = string.Empty;
    public required bool ShowNewMetrics { get; set; }
    public required bool AllowForEmployeesEditMetrics { get; set; }
    public required bool NotifEmail { get; set; }
    public required bool NotifInApp { get; set; }
    public required SystemNotificationTypeEnum NotifFrequency { get; set; }
    public required string UiLanguage { get; set; } = string.Empty;
    public required bool AutoUpdateFromAPI { get; set; }
}
