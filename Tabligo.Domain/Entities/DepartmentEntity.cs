using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Entities;

public class DepartmentEntity
{
    public int Id { get; set; }
    public required string Name { get; set; } = null!;

    public required int CompanyId { get; set; }
    public CompanyEntity Company { get; set; } = null!;

    public IEnumerable<EmployeeDepartmentLinkEntity> SupervisorsCalculatedLink => LinkEntities.Where(w => w.Type == EmployeeTypeEnum.Supervisor);
    public IEnumerable<EmployeeDepartmentLinkEntity> EmployeesCalculatedLink => LinkEntities.Where(w => w.Type == EmployeeTypeEnum.Employee);

    public required string Comment { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsFundamental { get; set; }
    public bool IsDeleted { get; set; }

    public List<EmployeeDepartmentLinkEntity> LinkEntities { get; set; } = new();
    public List<DepartmentSchemeEntity> OwnerSchemas { get; set; } = new();
    public List<DepartmentSchemeEntity> PartInSchemas { get; set; } = new();
    public List<MetricDepartmentLinkEntity> MetricLinks { get; set; } = new();
    
    public List<ExternalIdLinkEntity> ExternalIdLinks { get; set; } = new();
}
