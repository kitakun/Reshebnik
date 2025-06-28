using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Entities;

public class EmployeeDepartmentLinkEntity
{
    public int Id { get; set; }
    
    public EmployeeTypeEnum Type { get; set; }
    
    public int EmployeeId { get; set; }
    public EmployeeEntity Employee { get; set; } = null!;
    
    public int DepartmentId { get; set; }
    public DepartmentEntity Department { get; set; } = null!;
}