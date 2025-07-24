namespace Reshebnik.Domain.Models.Department;

public class DepartmentEmployeesUpsertDto
{
    public int DepartmentId { get; set; }
    public List<int> EmployeeIds { get; set; } = new();
    public List<int> SupervisorIds { get; set; } = new();
}
