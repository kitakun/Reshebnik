namespace Reshebnik.Domain.Models.Dashboard;

public class DashboardEmployeeDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public bool IsSupervisor { get; set; }
    public double Average { get; set; }
}
