using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Company;

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Industry { get; set; }
    public int EmployeesCount { get; set; }
    public CompanyTypeEnum Type { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public bool NotifyAboutLoweringMetrics { get; set; }
    public SystemNotificationTypeEnum NotificationType { get; set; }
    public string LanguageCode { get; set; } = null!;
}
