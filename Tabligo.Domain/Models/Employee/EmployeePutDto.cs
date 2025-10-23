namespace Tabligo.Domain.Models.Employee;

public class EmployeePutDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string? Email { get; set; }
    public string Phone { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int[] DepartmentIds { get; set; } = null!;
    public bool IsSupervisor { get; set; }
    public bool SendEmail { get; set; }
}