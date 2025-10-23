using Microsoft.EntityFrameworkCore;

using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Company;

public class SuAllCompanyIdsHandler(TabligoContext dbContext)
{
    public async ValueTask<Dictionary<int, string>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext
            .Companies
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, v => v.Name, cancellationToken: cancellationToken);
    }
}