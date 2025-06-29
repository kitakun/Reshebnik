namespace Reshebnik.Web.DTO.Employee;

using Reshebnik.Domain.Enums;

public class EmployeeCreateDto
{
    public int CompanyId { get; set; }
    public string Fio { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? EmailInvitationCode { get; set; }
    public string Salt { get; set; } = string.Empty;
    public RootRolesEnum Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
