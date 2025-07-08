namespace Reshebnik.Domain.Entities;

public class DepartmentSchemeEntity
{
    public int Id { get; set; }

    public int FundamentalDepartmentId { get; set; }
    public DepartmentEntity FundamentalDepartment { get; set; } = null!;

    public int AncestorDepartmentId { get; set; }
    // ↑ 
    public DepartmentEntity AncestorDepartment { get; set; } = null!;

    public int DepartmentId { get; set; }
    public DepartmentEntity Department { get; set; } = null!;

    public int Depth { get; set; }
}