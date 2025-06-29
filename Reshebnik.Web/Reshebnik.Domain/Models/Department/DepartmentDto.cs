namespace Reshebnik.Web.DTO.Department;

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsFundamental { get; set; }
}
