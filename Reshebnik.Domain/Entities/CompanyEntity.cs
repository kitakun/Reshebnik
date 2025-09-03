using Reshebnik.Domain.Enums;

using System.Text.Json.Serialization;

namespace Reshebnik.Domain.Entities;

public class CompanyEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Industry { get; set; }
    public int EmployeesCount { get; set; }
    public required CompanyTypeEnum Type { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }

    // settings
    public bool NotifyAboutLoweringMetrics { get; set; }
    public SystemNotificationTypeEnum NotificationType { get; set; }
    public string LanguageCode { get; set; } = null!;
    public PeriodTypeEnum Period { get; set; }
    public string DefaultMetrics { get; set; } = null!;
    public bool AutoUpdateByApi { get; set; }
    public bool AllowForEmployeesEditMetrics { get; set; }
    public bool EnableNotificationsInApp { get; set; }
    public bool ShowNewMetrics { get; set; }

    // refs
    [JsonIgnore]
    public List<EmployeeEntity> Employees { get; set; } = new();
    [JsonIgnore]
    public List<DepartmentEntity> Departments { get; set; } = new();
    [JsonIgnore]
    public List<MetricEntity> Metrics { get; set; } = new();
    [JsonIgnore]
    public List<ArchivedMetricEntity> ArchivedMetrics { get; set; } = new();
    [JsonIgnore]
    public List<ArchivedUserEntity> ArchivedUsers { get; set; } = new();
}