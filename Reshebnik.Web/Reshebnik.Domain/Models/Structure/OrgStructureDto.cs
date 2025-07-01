namespace Reshebnik.Domain.Models.Structure;

public class OrgStructureDto
{
    public string CompanyName { get; set; } = null!;
    public List<OrgUnitDto> Departments { get; set; } = new();
}
