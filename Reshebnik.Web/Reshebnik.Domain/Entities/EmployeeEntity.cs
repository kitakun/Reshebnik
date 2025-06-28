namespace Reshebnik.Domain.Entities;

public class EmployeeEntity
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public CompanyEntity Company { get; set; }

    public string FIO { get; set; }
    public string JobTitle { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Comment { get; set; }
    public bool IsActive { get; set; }
    public string? EmailInvitationCode { get; set; }
    
    public List<EmployeeDepartmentLinkEntity> DepartmentLinks { get; set; }
}