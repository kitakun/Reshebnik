namespace Reshebnik.Domain.Entities;

public class DepartmentEntity
{
    public int Id { get; set; }
    public string Name { get; set; }

    public IEnumerable<EmployeeDepartmentLinkEntity> SupervisorsCalculatedLink => LinkEntities.Where(w => w.Type == EmployeeTypeEnum.Supervisor);
    public IEnumerable<EmployeeDepartmentLinkEntity> EmployeesCalculatedLink => LinkEntities.Where(w => w.Type == EmployeeTypeEnum.Employee);

    public string Comment { get; set; }
    public bool IsActive { get; set; }
    public bool IsFundamental { get; set; }

    public List<EmployeeDepartmentLinkEntity> LinkEntities { get; set; }
    public List<DepartmentSchemeEntity> OwnerSchemas { get; set; }
    public List<DepartmentSchemeEntity> PartInSchemas { get; set; }
}