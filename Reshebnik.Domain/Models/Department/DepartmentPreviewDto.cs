namespace Reshebnik.Domain.Models.Department;

public class DepartmentPreviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Depth { get; set; }
    public double CompletionPercent { get; set; }
    public DepartmentPreviewMetricsDto Metrics { get; set; } = new();
    public List<DepartmentPreviewDto> Children { get; set; } = new();
    public List<DepartmentPreviewUserDto> Supervisors { get; set; } = new();
    public List<DepartmentPreviewUserDto> Employees { get; set; } = new();
    public List<DepartmentPreviewUserDto> BestEmployees { get; set; } = new();
    public List<DepartmentPreviewUserDto> WorstEmployees { get; set; } = new();
}
