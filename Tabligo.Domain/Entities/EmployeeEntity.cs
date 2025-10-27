using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Entities;

public class EmployeeEntity
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public CompanyEntity Company { get; set; } = null!;

    public string FIO { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string? Email { get; set; }
    public string Phone { get; set; } = null!;
    public string Comment { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public string? EmailInvitationCode { get; set; }

    public string Password { get; set; } = null!;
    public string Salt { get; set; } = null!;

    public RootRolesEnum Role { get; set; }
    public EmployeeTypeEnum? DefaultRole { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public bool? WelcomeWasSeen { get; set; }

    public List<EmployeeDepartmentLinkEntity> DepartmentLinks { get; set; } = new();
    public List<UserNotification> UserNotification { get; set; } = new();

    public List<MetricEmployeeLinkEntity> MetricLinks { get; set; } = new();
    public ArchivedUserEntity? ArchivedUser { get; set; }
    
    public List<ExternalIdLinkEntity> ExternalIdLinks { get; set; } = new();
}