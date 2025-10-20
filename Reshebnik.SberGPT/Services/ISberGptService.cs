namespace Reshebnik.SberGPT.Services;

public interface ISberGptService
{
    Task<string> HandleAsync(string request);
    IAsyncEnumerable<string> HandleStreamAsync(string request, CancellationToken cancellationToken = default);
}
