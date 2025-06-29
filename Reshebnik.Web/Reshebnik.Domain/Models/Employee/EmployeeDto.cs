namespace Reshebnik.Web.DTO.Employee;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
