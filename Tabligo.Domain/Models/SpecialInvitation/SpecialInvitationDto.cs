namespace Tabligo.Domain.Models.SpecialInvitation;

public class SpecialInvitationDto
{
    public int Id { get; set; }
    public string Fio { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public string CompanyDescription { get; set; } = null!;
    public int CompanySize { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Granted { get; set; }
}
