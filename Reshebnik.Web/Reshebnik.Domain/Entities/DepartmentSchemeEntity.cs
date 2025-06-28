namespace Reshebnik.Domain.Entities;

public class DepartmentSchemeEntity
{
    public int Id { get; set; }

    public int FundamentalDepartmentId { get; set; }
    public DepartmentEntity FundamentalDepartment { get; set; }

    public int AncestorDepartmentId { get; set; }
    // ↑ 
    public DepartmentEntity AncestorDepartment { get; set; }

    public int DepartmentId { get; set; }
    public DepartmentEntity Department { get; set; }

    public int Depth { get; set; }
}