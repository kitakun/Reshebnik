namespace Tabligo.Domain.Models.BugHunt;

public class BugHuntCreateRequest
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Screenshot { get; set; }
    public BugHuntLastRequest? LastRequest { get; set; }
}

public class BugHuntLastRequest
{
    public string? Url { get; set; }
    public object? Response { get; set; }
    public int? Status { get; set; }
}
