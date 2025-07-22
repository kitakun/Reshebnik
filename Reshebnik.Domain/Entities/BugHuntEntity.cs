namespace Reshebnik.Domain.Entities;

public class BugHuntEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Screenshot { get; set; }
    public string? LastRequestUrl { get; set; }
    public string? LastRequestResponse { get; set; }
    public int? LastRequestStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
