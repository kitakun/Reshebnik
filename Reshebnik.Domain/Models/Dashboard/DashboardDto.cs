namespace Reshebnik.Domain.Models.Dashboard;

public class DashboardDto
{
    public List<DashboardMetricDto> Metrics { get; set; } = new();
    public List<DashboardEmployeeDto> BestEmployees { get; set; } = new();
    public List<DashboardEmployeeDto> WorstEmployees { get; set; } = new();
    public List<DashboardDepartmentDto> Departments { get; set; } = new();
    public double DepartmentsAverage { get; set; }
}
