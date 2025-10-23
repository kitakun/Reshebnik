using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Employee;

public class EmployeeCreateDto
{
    public required int CompanyId { get; set; }
    public required string Fio { get; set; }
    public required string JobTitle { get; set; }
    public string? Email { get; set; }
    public required string Phone { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsSupervisor { get; set; }
    public string? EmailInvitationCode { get; set; }
    public string Salt { get; set; } = string.Empty;
    public required RootRolesEnum Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}
