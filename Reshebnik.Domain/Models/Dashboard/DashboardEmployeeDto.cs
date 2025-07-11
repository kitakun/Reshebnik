namespace Reshebnik.Domain.Models.Dashboard;

public class DashboardEmployeeDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public double Average { get; set; }
}
