namespace Reshebnik.Domain.Entities;

public class CategoryRecordEntity
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public CompanyEntity Company { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
}
