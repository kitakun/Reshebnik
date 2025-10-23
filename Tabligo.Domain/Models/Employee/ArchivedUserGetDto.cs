namespace Tabligo.Domain.Models.Employee;

public class ArchivedUserGetDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime ArchivedAt { get; set; }
    public int ArchivedByUserId { get; set; }
}
