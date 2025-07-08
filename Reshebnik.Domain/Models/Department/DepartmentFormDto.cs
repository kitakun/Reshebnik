namespace Reshebnik.Domain.Models.Department;

public class DepartmentFormDto
{
    public string Name { get; set; } = null!;
    public int? SupervisorId { get; set; }
    public int? ParentId { get; set; }
    public List<int> EmployeeIds { get; set; } = new();
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
