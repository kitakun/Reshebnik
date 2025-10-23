namespace Tabligo.Domain.Entities;

public class ArchivedUserEntity
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public EmployeeEntity Employee { get; set; } = null!;
    public DateTime ArchivedAt { get; set; }
    public int ArchivedByUserId { get; set; }
    public EmployeeEntity ArchivedByUser { get; set; } = null!;
}
