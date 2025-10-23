using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Department;

namespace Tabligo.Domain.Models.Employee;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string? Email { get; set; }
    public string Phone { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSupervisor { get; set; }
    public required DepartmentShortDto[] Departments { get; set; }
    public EmployeeTypeEnum? DefaultRole { get; set; }
}
