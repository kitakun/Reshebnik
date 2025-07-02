using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Models.Employee;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public required string? DepartmentName { get; set; }
    public required int? DepartmentId { get; set; }
    public EmployeeTypeEnum? DefaultRole { get; set; }
}
