namespace Tabligo.Domain.Models.Department;

public class DepartmentPreviewUserDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public bool IsSupervisor { get; set; }
    public double CompletionPercent { get; set; }
}
