namespace Reshebnik.Domain.Models.Department;

public class DepartmentPreviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public double CompletionPercent { get; set; }
    public List<DepartmentPreviewDto> Children { get; set; } = new();
    public List<DepartmentPreviewUserDto> Supervisors { get; set; } = new();
    public List<DepartmentPreviewUserDto> BestEmployees { get; set; } = new();
    public List<DepartmentPreviewUserDto> WorstEmployees { get; set; } = new();
}
