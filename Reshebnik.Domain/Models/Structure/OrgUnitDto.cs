namespace Reshebnik.Domain.Models.Structure;

public class OrgUnitDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<OrgUnitDto> Children { get; set; } = new();
}
