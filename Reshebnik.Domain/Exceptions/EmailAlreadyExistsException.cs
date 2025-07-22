namespace Reshebnik.Domain.Exceptions;

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException() : base("E-Mail уже занят")
    {
    }
}
