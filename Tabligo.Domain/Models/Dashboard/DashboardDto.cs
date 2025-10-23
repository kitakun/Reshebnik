namespace Tabligo.Domain.Models.Dashboard;

public class DashboardDto
{
    public List<DashboardMetricDto> Metrics { get; set; } = new();
    public List<DashboardEmployeeDto> BestEmployees { get; set; } = new();
    public List<DashboardEmployeeDto> WorstEmployees { get; set; } = new();
    public List<DashboardDepartmentDto> Departments { get; set; } = new();
    public double DepartmentsAverage { get; set; }
    /// <summary>
    /// Average value in percent from all key indicators (including hidden ones)
    /// </summary>
    public double KeyIndicatorAverage { get; set; }
}
