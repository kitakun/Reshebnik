
using Tabligo.Domain.Models.Neural;

namespace Tabligo.Neural.Interfaces;

public interface ITabligoNeuralAgent
{
    /// <summary>
    /// Processes a file and returns suggested entities to create based on the file content
    /// </summary>
    /// <param name="fileContent">The content of the uploaded file</param>
    /// <param name="fileName">The name of the uploaded file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Neural response with suggested entities</returns>
    Task<NeuralResponse> ProcessFileAsync(string fileContent, string fileName, CancellationToken cancellationToken = default);
}
