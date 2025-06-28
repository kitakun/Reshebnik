using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;

namespace Reshebnik.Web.DTO.Auth;

public class AdminLoginResponse(EmployeeEntity User, string Jwt, int? currentCompanyId)
{
    public int Id { get; set; } = User.Id;
    public string Username { get; set; } = User.FIO;
    public string CompanyName { get; set; } = User.Company.Name;
    public RootRolesEnum Role { get; set; } = User.Role;
    public bool IsActive { get; set; } = User.IsActive;
    public string Jwt { get; set; } = Jwt;
    public int? CompanyId { get; set; } = currentCompanyId;
    public bool IsReadonly { get; set; } = User.Email == "demo";
}