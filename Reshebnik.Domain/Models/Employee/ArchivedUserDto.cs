namespace Reshebnik.Domain.Models.Employee;

public class ArchivedUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime ArchivedAt { get; set; }
    public int EmployeeId { get; set; }
}
