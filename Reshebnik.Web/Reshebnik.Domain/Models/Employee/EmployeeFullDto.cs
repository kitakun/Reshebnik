using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Company;

namespace Reshebnik.Domain.Models.Employee;

public class EmployeeFullDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public RootRolesEnum Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }

    public CompanyDto Company { get; set; } = null!;
}
