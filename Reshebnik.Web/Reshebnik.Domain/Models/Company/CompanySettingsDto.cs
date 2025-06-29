namespace Reshebnik.Domain.Models.Company;

public class CompanySettingsDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string LegalType { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string DefaultMetric { get; set; } = string.Empty;
    public bool ShowNewMetrics { get; set; }
    public bool AllowEditMetrics { get; set; }
    public bool NotifEmail { get; set; }
    public bool NotifInApp { get; set; }
    public string NotifFrequency { get; set; } = string.Empty;
    public string UiLanguage { get; set; } = string.Empty;
    public bool AutoUpdate { get; set; }
}
