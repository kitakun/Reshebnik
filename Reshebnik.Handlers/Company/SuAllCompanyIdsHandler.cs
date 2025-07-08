using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Company;

public class SuAllCompanyIdsHandler(ReshebnikContext dbContext)
{
    public async ValueTask<Dictionary<int, string>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext
            .Companies
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, v => v.Name, cancellationToken: cancellationToken);
    }   
}