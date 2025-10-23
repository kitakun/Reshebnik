using Tabligo.Domain.Models.Structure;
using Tabligo.Domain.Models.Department;
using Tabligo.Handlers.Department;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Structure;

public class StructureGetHandler(
    DepartmentGetHandler departmentHandler,
    CompanyContextHandler companyContext)
{
    public async Task<OrgStructureDto?> HandleAsync(CancellationToken ct = default)
    {
        var company = await companyContext.CurrentCompanyAsync;
        if (company == null) return null;

        var departments = await departmentHandler.HandleAsync(ct);
        var units = departments.Select(MapUnit).ToList();

        return new OrgStructureDto
        {
            CompanyName = company.Name,
            Departments = units
        };
    }

    private static OrgUnitDto MapUnit(DepartmentTreeDto dto)
    {
        return new OrgUnitDto
        {
            Id = dto.Id ?? 0,
            Name = dto.Name,
            Children = dto.Children.Select(MapUnit).ToList()
        };
    }
}
