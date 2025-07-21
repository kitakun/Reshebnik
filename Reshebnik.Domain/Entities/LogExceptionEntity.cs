namespace Reshebnik.Domain.Entities;

public class LogExceptionEntity
{
    public int Id { get; set; }

    public string Message { get; set; } = null!;

    public string StackTrace { get; set; } = null!;

    public string? UserEmail { get; set; }

    public int? CompanyId { get; set; }
    public CompanyEntity? Company { get; set; }
}
