namespace Reshebnik.Domain.Models;

public record LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}