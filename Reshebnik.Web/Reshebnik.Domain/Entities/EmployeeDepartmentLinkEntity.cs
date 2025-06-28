namespace Reshebnik.Domain.Entities;

public class EmployeeDepartmentLinkEntity
{
    public int Id { get; set; }
    
    public EmployeeTypeEnum Type { get; set; }
    
    public int EmployeeId { get; set; }
    public EmployeeEntity Employee { get; set; }
    
    public int DepartmentId { get; set; }
    public DepartmentEntity Department { get; set; }
}