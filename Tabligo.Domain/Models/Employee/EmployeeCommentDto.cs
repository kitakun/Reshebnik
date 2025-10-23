namespace Tabligo.Domain.Models.Employee;

public class EmployeeCommentDto
{
    public string? JobTitle { get; set; } = null!;
    public bool? IsSupervisor { get; set; }
    public string Comment { get; set; } = string.Empty;
}
