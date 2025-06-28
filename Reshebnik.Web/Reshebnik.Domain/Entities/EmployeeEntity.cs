using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Entities;

public class EmployeeEntity
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public CompanyEntity Company { get; set; } = null!;

    public string FIO { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Comment { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? EmailInvitationCode { get; set; }
    
    public string Password { get; set; } = null!;
    public string Salt { get; set; } = null!;
    
    public RootRolesEnum Role { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }

    public List<EmployeeDepartmentLinkEntity> DepartmentLinks { get; set; } = new();
    public List<UserNotification> UserNotification { get; set; } = new();
}