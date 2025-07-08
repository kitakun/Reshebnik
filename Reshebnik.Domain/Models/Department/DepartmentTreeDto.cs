namespace Reshebnik.Domain.Models.Department;

public class DepartmentTreeDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsFundamental { get; set; }
    public List<DepartmentUserDto> Users { get; set; } = new();
    public List<DepartmentTreeDto> Children { get; set; } = new();
}
