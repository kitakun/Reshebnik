namespace Reshebnik.Domain.Models.SpecialInvitation;

public class SpecialInvitationCreateDto
{
    public string Fio { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public string CompanyDescription { get; set; } = null!;
    public int CompanySize { get; set; }
}
