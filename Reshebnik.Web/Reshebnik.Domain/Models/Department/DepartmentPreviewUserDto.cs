namespace Reshebnik.Domain.Models.Department;

public class DepartmentPreviewUserDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public double CompletionPercent { get; set; }
}
