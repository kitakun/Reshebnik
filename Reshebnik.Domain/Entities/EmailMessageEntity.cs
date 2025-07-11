namespace Reshebnik.Domain.Entities;

public class EmailMessageEntity
{
    public int Id { get; set; }

    public string To { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public bool IsHtml { get; set; } = true;

    public string? Error { get; set; }

    // Optional fields
    public string? From { get; set; } = "no-reply@tabligo.ru";
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
    public List<EmailAttachment> Attachments { get; set; } = new();

    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }

    public required int SentByUserId { get; set; }
    public EmployeeEntity SentByUser { get; set; } = default!;

    public required int SentByCompanyId { get; set; }
    public CompanyEntity SentByCompany { get; set; }
}

public class EmailAttachment
{
    public int Id { get; set; }
    public string FileName { get; set; } = default!;
    public byte[] Content { get; set; } = default!;
    public string ContentType { get; set; } = "application/octet-stream";
}