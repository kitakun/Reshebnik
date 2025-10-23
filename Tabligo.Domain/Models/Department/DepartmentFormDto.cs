namespace Tabligo.Domain.Models.Department;

public class DepartmentFormDto
{
    public string Name { get; set; } = null!;
    public List<int> SupervisorIds { get; set; } = new();
    public int? ParentId { get; set; }
    public List<int> EmployeeIds { get; set; } = new();
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
